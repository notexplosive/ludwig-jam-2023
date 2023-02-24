using System;
using System.Collections.Generic;
using ExplogineCore;
using ExplogineCore.Data;
using ExplogineMonoGame;
using ExplogineMonoGame.AssetManagement;
using ExplogineMonoGame.Cartridges;
using ExplogineMonoGame.Data;
using ExplogineMonoGame.Gui;
using ExplogineMonoGame.Layout;
using ExTween;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LudJam;

public class LudGameCartridge : NoProviderCartridge, ILoadEventProvider
{
    public static LudGameCartridge Instance = null!;
    private readonly Camera _camera = new(new Vector2(1920, 1080));
    private readonly List<Vector2> _cameraFocusObjects = new();
    private readonly Gui _creditsGui = new();
    private readonly TweenableFloat _curtainPercent = new();
    private readonly Wrapped<bool> _fullscreenSetting = new();
    private readonly Gui _levelSelectGui = new();
    private readonly SequenceTween _levelTransitionTween = new();
    private readonly Gui _mainMenuGui = new();
    private string? _cachedLevelJson;
    private string? _cachedLevelName;
    private RectangleF _cameraTargetRect;
    private Level? _currentLevel;
    private int _currentLevelIndex;
    private bool _isEditorSession;
    private SimpleGuiTheme _levelSelectGuiTheme;
    private SimpleGuiTheme _mainGuiTheme = null!;
    private GameMode _mode;
    private float _totalElapsedTime;

    public LudGameCartridge(IRuntime runtime) : base(runtime)
    {
        LudGameCartridge.Instance = this;
    }

    public override CartridgeConfig CartridgeConfig => new(new Point(1920, 1080), SamplerState.AnisotropicWrap);
    public static Scale2D ActorScale => new(new Vector2(0.25f, 0.25f));

    public static string[] LevelSequence { get; private set; } = Array.Empty<string>();

    public IEnumerable<ILoadEvent> LoadEvents(Painter painter)
    {
        yield return new AssetLoadEvent("Sheet",
            () => new GridBasedSpriteSheet(Client.Assets.GetTexture("cat/sheet"),
                new Point(LudEditorCartridge.TextureFrameSize)));

        yield return new AssetLoadEvent("Brick",
            () => new TextureAsset(Client.Graphics.CropTexture(
                new Rectangle(0, 0, LudEditorCartridge.TextureFrameSize, LudEditorCartridge.TextureFrameSize),
                Client.Assets.GetTexture("cat/sheet"))));

        yield return new AssetLoadEvent("Background",
            () => new TextureAsset(Client.Graphics.CropTexture(
                new Rectangle(LudEditorCartridge.TextureFrameSize, 0, LudEditorCartridge.TextureFrameSize,
                    LudEditorCartridge.TextureFrameSize),
                Client.Assets.GetTexture("cat/sheet"))));

        yield return new VoidLoadEvent("LevelSequence",
            () =>
            {
                LudGameCartridge.LevelSequence =
                    Runtime.FileSystem.Local.ReadFile("Content/cat/level-sequence.txt").SplitLines();
            });
    }

    public override void OnCartridgeStarted()
    {
        G.Music.FadeToMain();
        _mode = GameMode.MainMenu;
        _mainGuiTheme =
            new SimpleGuiTheme(Color.White, Color.Black, Color.White, Client.Assets.GetFont("cat/Font", 72));
        _levelSelectGuiTheme =
            new SimpleGuiTheme(Color.White, Color.Black, Color.White, Client.Assets.GetFont("cat/Font", 32));

        var buttonSize = new Vector2(500, 150);
        var buttonX = 100;
        var buttonY = 800;

        _mainMenuGui.Button(new RectangleF(new Vector2(buttonX, buttonY), buttonSize), "Play", Depth.Middle, () =>
        {
            _mode = GameMode.Playing;
            G.Music.FadeToMain();
            LoadCurrentLevel();
            HideCurtain(1);
        });

        _mainMenuGui.Button(new RectangleF(new Vector2(buttonX + 1200, buttonY), buttonSize), "Editor", Depth.Middle,
            () => { LoadLevelEditor(); });

        _mainMenuGui.Button(new RectangleF(new Vector2(buttonX + 600, buttonY), buttonSize), "Level Select",
            Depth.Middle, () => { _mode = GameMode.LevelSelect; });

        _mainMenuGui.Button(new RectangleF(new Vector2(buttonX + 1200, buttonY - 170), new Vector2(500, 100)),
            "Credits",
            Depth.Middle, () =>
            {
                _mode = GameMode.Credits;
                HideCurtain();
            });

        _creditsGui.Button(new RectangleF(new Vector2(buttonX + 1200, buttonY), buttonSize), "Back", Depth.Middle,
            () =>
            {
                _mode = GameMode.MainMenu;
                ShowCurtain();
            });

        _levelSelectGui.Button(new RectangleF(new Vector2(buttonX + 1200, buttonY), buttonSize), "Back", Depth.Middle,
            () => { _mode = GameMode.MainMenu; });

        var safeZone = Runtime.Window.RenderResolution.ToRectangleF().Inflated(-100, -100);

        _mainMenuGui.DynamicLabel(safeZone, Depth.Middle,
            (painter, theme, rectangle, depth) =>
            {
                painter.DrawStringWithinRectangle(Client.Assets.GetFont("cat/Font", 200), "Super Pet The Cat", rectangle,
                    Alignment.TopLeft, new DrawSettings());

                painter.DrawStringWithinRectangle(Client.Assets.GetFont("cat/Font", 64),
                    "Made in like 5 days by NotExplosive", rectangle.Moved(new Vector2(0, 250)),
                    Alignment.TopLeft, new DrawSettings());
            });

        _creditsGui.DynamicLabel(safeZone, Depth.Middle,
            (painter, theme, rectangle, depth) =>
            {
                painter.DrawStringWithinRectangle(Client.Assets.GetFont("cat/Font", 64),
                    "Music by Crashtroid\nSome art from game-icons.net\nEverything else by NotExplosive\n\nMade with MonoGame and Explogine\n\nPlay more of my games at notexplosive.net",
                    rectangle, Alignment.CenterLeft, new DrawSettings());
            });

        _mainMenuGui.Checkbox(new RectangleF(new Vector2(safeZone.Left, 550), new Vector2(600, 72)), "Fullscreen",
            Depth.Middle, _fullscreenSetting);
        _fullscreenSetting.Value = Runtime.Window.IsFullscreen;
        _fullscreenSetting.ValueChanged += b => { Runtime.Window.SetFullscreen(b); };

        _mainMenuGui.Label(new RectangleF(new Vector2(safeZone.Left, 650), new Vector2(600, 72)), Depth.Middle,
            "Volume");
        _mainMenuGui.Slider(new RectangleF(new Vector2(safeZone.Left + 250, 650), new Vector2(600, 72)),
            Orientation.Horizontal, 20, Depth.Middle, G.Music.VolumeInt);

        var layoutBuilder =
            new LayoutBuilder(new Style(Orientation.Vertical, 10, new Vector2(20, 20), Alignment.Center));

        foreach (var _ in LudGameCartridge.LevelSequence)
        {
            layoutBuilder.Add(L.FillVertical("button", buttonSize.X));
        }

        var layout = layoutBuilder.Bake(Runtime.Window.RenderResolution);

        var list = layout.FindElements("button");
        for (var levelIndex = 0; levelIndex < list.Count; levelIndex++)
        {
            var buttonElement = list[levelIndex];
            var captured = levelIndex;
            _levelSelectGui.Button(buttonElement.Rectangle, LudGameCartridge.LevelSequence[levelIndex], Depth.Middle,
                () =>
                {
                    _currentLevelIndex = captured;

                    // same as play button
                    _mode = GameMode.Playing;
                    G.Music.FadeToMain();
                    LoadCurrentLevel();
                    HideCurtain(1);
                });
        }
    }

    private void LoadCurrentLevel()
    {
        G.Music.FadeToMain(1f);
        if (_isEditorSession)
        {
            LoadCachedEditorLevel();
            return;
        }

        _currentLevel = LoadLevel(_currentLevelIndex);
    }

    private Level? LoadLevel(int i)
    {
        if (i < LudGameCartridge.LevelSequence.Length)
        {
            var levelName = LudGameCartridge.LevelSequence[i];
            var levelData = G.EditorDevelopmentFileSystem(Runtime).ReadFile($"Content/cat/{levelName}.json");
            _cachedLevelName = levelName;
            return new Level().LoadFromJson(levelData, true).FinishLoadingLevelForGame();
        }

        _currentLevelIndex = 0;
        _mode = GameMode.Credits;
        return null;
    }

    public override void Draw(Painter painter)
    {
        G.DrawBackground(painter, Runtime.Window.RenderResolution, _camera);

        var shader = Client.Assets.GetEffect("cat/Shadow");
        painter.BeginSpriteBatch(_camera.CanvasToScreen * Matrix.CreateTranslation(new Vector3(new Vector2(10), 0)),
            shader);
        _currentLevel?.Scene.DrawContent(painter);
        painter.EndSpriteBatch();

        painter.BeginSpriteBatch(_camera.CanvasToScreen);
        _currentLevel?.Scene.DrawContent(painter);
        painter.EndSpriteBatch();

        // par text shadow
        painter.BeginSpriteBatch(Matrix.CreateTranslation(new Vector3(new Vector2(4, 4), 0)), shader);
        DrawParText(painter);
        painter.EndSpriteBatch();

        // par text
        painter.BeginSpriteBatch();
        DrawParText(painter);
        painter.EndSpriteBatch();

        painter.BeginSpriteBatch();
        var curtainRectangle = Runtime.Window.RenderResolution.ToRectangleF();
        var curtainX = curtainRectangle.Width * _curtainPercent.Value;
        curtainRectangle.Location = new Vector2(curtainX, 0);
        painter.DrawRectangle(curtainRectangle, new DrawSettings {Color = G.CurtainColor2});
        painter.DrawAsRectangle(Client.Assets.GetTexture("Background"), curtainRectangle,
            new DrawSettings
            {
                SourceRectangle = new Rectangle(new Point((int) (_totalElapsedTime * 1000)),
                    (curtainRectangle.Size * LudEditorCartridge.BackgroundScalar).ToPoint()),
                Color = G.CurtainColor1
            });
        painter.EndSpriteBatch();

        if (_mode == GameMode.MainMenu)
        {
            // todo: extract this into a function because we copy-paste it below

            // shadow
            _mainMenuGui.PrepareCanvases(painter, _mainGuiTheme);
            painter.BeginSpriteBatch(Matrix.CreateTranslation(new Vector3(new Vector2(10, 10), 0)), shader);
            _mainMenuGui.Draw(painter, _mainGuiTheme);
            painter.EndSpriteBatch();

            // real
            _mainMenuGui.PrepareCanvases(painter, _mainGuiTheme);
            painter.BeginSpriteBatch(Matrix.Identity);
            _mainMenuGui.Draw(painter, _mainGuiTheme);
            painter.EndSpriteBatch();
        }

        if (_mode == GameMode.LevelSelect)
        {
            // shadow
            _levelSelectGui.PrepareCanvases(painter, _levelSelectGuiTheme);
            painter.BeginSpriteBatch(Matrix.CreateTranslation(new Vector3(new Vector2(10, 10), 0)), shader);
            _levelSelectGui.Draw(painter, _levelSelectGuiTheme);
            painter.EndSpriteBatch();

            // real
            _levelSelectGui.PrepareCanvases(painter, _levelSelectGuiTheme);
            painter.BeginSpriteBatch(Matrix.Identity);
            _levelSelectGui.Draw(painter, _levelSelectGuiTheme);
            painter.EndSpriteBatch();
        }

        if (_mode == GameMode.Credits)
        {
            // shadow
            _creditsGui.PrepareCanvases(painter, _mainGuiTheme);
            painter.BeginSpriteBatch(Matrix.CreateTranslation(new Vector3(new Vector2(10, 10), 0)), shader);
            _creditsGui.Draw(painter, _mainGuiTheme);
            painter.EndSpriteBatch();

            // real
            _creditsGui.PrepareCanvases(painter, _mainGuiTheme);
            painter.BeginSpriteBatch(Matrix.Identity);
            _creditsGui.Draw(painter, _mainGuiTheme);
            painter.EndSpriteBatch();
        }
    }

    private void DrawParText(Painter painter)
    {
        var safeAreaRect = Runtime.Window.RenderResolution.ToRectangleF().Inflated(-20, -20);
        if (_currentLevel != null)
        {
            var color = _currentLevel.IsPassedPar ? Color.OrangeRed : Color.White;
            painter.DrawStringWithinRectangle(Client.Assets.GetFont("cat/Font", 80),
                _currentLevel.ParStatus(), safeAreaRect, Alignment.TopCenter,
                new DrawSettings
                    {Color = color, Depth = Depth.Middle});

            painter.DrawStringWithinRectangle(Client.Assets.GetFont("cat/Font", 40),
                "notexplosive.net", safeAreaRect, Alignment.BottomCenter,
                new DrawSettings
                    {Color = Color.White, Depth = Depth.Middle});
        }
    }

    public override void Update(float dt)
    {
        if (_cameraFocusObjects.Count > 0)
        {
            var totalPosition = Vector2.Zero;

            foreach (var position in _cameraFocusObjects)
            {
                totalPosition += position;
            }

            var averagePosition = totalPosition / _cameraFocusObjects.Count;

            var longestLength = 0f;
            foreach (var position in _cameraFocusObjects)
            {
                longestLength = MathF.Max(longestLength, (averagePosition - position).Length());
            }

            var totalRectangle = new RectangleF(averagePosition, Vector2.Zero);
            totalRectangle = totalRectangle.Inflated(16, 9);
            totalRectangle = totalRectangle.InflatedMaintainAspectRatio(longestLength);

            // safe zone
            totalRectangle = totalRectangle.InflatedMaintainAspectRatio(Math.Min(100, longestLength / 2f));

            _cameraTargetRect = totalRectangle;
            _cameraFocusObjects.Clear();
        }
        else
        {
            _cameraTargetRect = Runtime.Window.RenderResolution.ToRectangleF();
        }

        _cameraTargetRect = _cameraTargetRect.InflatedMaintainAspectRatio(100);
        _camera.ViewBounds = TweenableRectangleF.LerpRectangleF(_camera.ViewBounds, _cameraTargetRect, 0.1f);

        _totalElapsedTime += dt;
        _levelTransitionTween.Update(dt);

        if (G.ImpactTimer > 0)
        {
            G.ImpactTimer -= dt;
            return;
        }

        _currentLevel?.Scene.Update(dt);
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
        if (_mode == GameMode.MainMenu)
        {
            _mainMenuGui.UpdateInput(input, hitTestStack);
        }

        if (_mode == GameMode.LevelSelect)
        {
            _levelSelectGui.UpdateInput(input, hitTestStack);
        }

        if (input.Keyboard.Modifiers.ControlShift && input.Keyboard.GetButton(Keys.R, true).WasPressed)
        {
            LudCoreCartridge.Instance.RegenerateCartridge<LudGameCartridge>();
        }

        if (_mode == GameMode.Playing)
        {
            if (input.Keyboard.GetButton(Keys.F4, true).WasPressed && _currentLevel != null)
            {
                var editor = LoadLevelEditor();
                editor.LoadJson(_currentLevel.ToJson(), _cachedLevelName);
            }

            if (!_levelTransitionTween.IsDone())
            {
                return;
            }

            var worldHitTestStack = hitTestStack.AddLayer(_camera.ScreenToCanvas, Depth.Middle);
            _currentLevel?.Scene.UpdateInput(input, worldHitTestStack);

            if (input.Keyboard.GetButton(Keys.R, true).WasPressed)
            {
                LoadCurrentLevel();
            }
        }

        if (_mode == GameMode.Credits)
        {
            _creditsGui.UpdateInput(input, hitTestStack);
        }
    }

    private LudEditorCartridge LoadLevelEditor()
    {
        LudCoreCartridge.Instance.RegenerateCartridge<LudEditorCartridge>();
        return LudCoreCartridge.Instance.SwapTo<LudEditorCartridge>();
    }

    public static Vector2 GetRandomVector(NoiseBasedRng random)
    {
        return random.NextPositiveVector2()
            .StraightMultiply(new Vector2(random.NextSign(), random.NextSign()));
    }

    public void MoveToNextLevel()
    {
        _currentLevelIndex++;
        LoadCurrentLevel();
        HideCurtain();
    }

    private void HideCurtain(float delay = 0)
    {
        _levelTransitionTween.Clear();
        _curtainPercent.Value = 0;

        _levelTransitionTween.Add(new WaitSecondsTween(delay));
        _levelTransitionTween.Add(new WaitSecondsTween(0.1f));
        _levelTransitionTween.Add(new CallbackTween(() => { _camera.ViewBounds = _cameraTargetRect; }));
        _levelTransitionTween.Add(_curtainPercent.TweenTo(1f, G.TransitionDuration, Ease.QuadSlowFast));
    }

    public void TransitionToNextLevel()
    {
        ShowCurtain();
        _levelTransitionTween.Add(new CallbackTween(() => { LudGameCartridge.Instance.MoveToNextLevel(); }));
    }

    private void ShowCurtain()
    {
        _levelTransitionTween.Clear();
        _curtainPercent.Value = -1;
        _levelTransitionTween.Add(_curtainPercent.TweenTo(0f, G.TransitionDuration, Ease.QuadFastSlow));
    }

    public void ResetCurrentLevel()
    {
        ShowCurtain();
        _levelTransitionTween.Add(new CallbackTween(LoadCurrentLevel));
        _levelTransitionTween.Add(new CallbackTween(() => HideCurtain()));
    }

    public void AddCameraFocusPoint(Vector2 point)
    {
        _cameraFocusObjects.Add(point);
    }

    public void LoadJson(string levelJson, string? levelName)
    {
        _cachedLevelJson = levelJson;
        _currentLevel = new Level().LoadFromJson(levelJson, true);
        _cachedLevelName = levelName;
        _isEditorSession = true;
        _mode = GameMode.Playing;
        HideCurtain();
    }

    private void LoadCachedEditorLevel()
    {
        LoadJson(_cachedLevelJson!, _cachedLevelName);
    }
}

public enum GameMode
{
    MainMenu,
    Playing,
    Paused,
    Credits,
    LevelSelect
}
