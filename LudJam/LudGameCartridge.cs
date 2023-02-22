using System;
using System.Collections.Generic;
using ExplogineCore;
using ExplogineCore.Data;
using ExplogineMonoGame;
using ExplogineMonoGame.AssetManagement;
using ExplogineMonoGame.Cartridges;
using ExplogineMonoGame.Data;
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
    private readonly TweenableFloat _curtainPercent = new();
    private readonly SequenceTween _levelTransitionTween = new();
    private RectangleF _cameraTargetRect;
    private Level? _currentLevel;
    private int _currentLevelIndex;
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
        LoadCurrentLevel();
        ClearCurtain();
    }

    private void LoadCurrentLevel()
    {
        _currentLevel = LoadLevel(_currentLevelIndex);
    }

    private Level LoadLevel(int i)
    {
        if (i < LudGameCartridge.LevelSequence.Length)
        {
            var levelName = LudGameCartridge.LevelSequence[i];
            var levelData = G.EditorDevelopmentFileSystem(Runtime).ReadFile($"Content/cat/{levelName}.json");
            return new Level().LoadFromJson(levelData, true).FinishLoadingLevelForGame();
        }

        throw new Exception("Ran out of levels! ... I need to make an outro screen");
    }

    public override void Draw(Painter painter)
    {
        G.DrawBackground(painter, Runtime.Window.RenderResolution, _camera);

        // only draw the drop shadow on release builds because shaders break hot reload (that sucks!)
        painter.BeginSpriteBatch(_camera.CanvasToScreen * Matrix.CreateTranslation(new Vector3(new Vector2(10), 0)),
            Client.Assets.GetEffect("cat/Shadow"));
        _currentLevel?.Scene.DrawContent(painter);
        painter.EndSpriteBatch();

        painter.BeginSpriteBatch(_camera.CanvasToScreen);
        _currentLevel?.Scene.DrawContent(painter);
        painter.EndSpriteBatch();

        painter.BeginSpriteBatch();
        var safeAreaRect = Runtime.Window.RenderResolution.ToRectangleF().Inflated(-20, -20);
        painter.DrawStringWithinRectangle(Client.Assets.GetFont("cat/Font", 80),
            _currentLevel?.ParStatus() ?? "No level", safeAreaRect, Alignment.TopCenter,
            new DrawSettings {Color = _currentLevel.IsPassedPar ? Color.OrangeRed : Color.White, Depth = Depth.Middle});
        painter.DrawStringWithinRectangle(Client.Assets.GetFont("cat/Font", 80),
            _currentLevel?.ParStatus() ?? "No level", safeAreaRect.Moved(new Vector2(2)), Alignment.TopCenter,
            new DrawSettings {Depth = Depth.Middle + 1, Color = Color.Black});
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
            totalRectangle = totalRectangle.InflatedMaintainAspectRatio(100);

            _cameraTargetRect = totalRectangle;
            _cameraFocusObjects.Clear();
        }

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
        if (input.Keyboard.GetButton(Keys.F4, true).WasPressed && _currentLevel != null)
        {
            LudCoreCartridge.Instance.RegenerateCartridge<LudEditorCartridge>();
            var editor = LudCoreCartridge.Instance.SwapTo<LudEditorCartridge>();
            editor.LoadJson(_currentLevel.ToJson());
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

    public static Vector2 GetRandomVector(NoiseBasedRng random)
    {
        return random.NextPositiveVector2()
            .StraightMultiply(new Vector2(random.NextSign(), random.NextSign()));
    }

    public void MoveToNextLevel()
    {
        ClearCurtain();

        _currentLevelIndex++;
        LoadCurrentLevel();
    }

    private void ClearCurtain()
    {
        _levelTransitionTween.Clear();
        _curtainPercent.Value = 0;
        _levelTransitionTween.Add(_curtainPercent.TweenTo(1f, G.TransitionDuration, Ease.QuadFastSlow));
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
        _levelTransitionTween.Add(_curtainPercent.TweenTo(0f, G.TransitionDuration, Ease.QuadSlowFast));
    }

    public void ResetLevelAfterTimer()
    {
        ShowCurtain();
        _levelTransitionTween.Add(new CallbackTween(LoadCurrentLevel));
        _levelTransitionTween.Add(new CallbackTween(ClearCurtain));
    }

    public void AddCameraFocusPoint(Vector2 point)
    {
        _cameraFocusObjects.Add(point);
    }
}
