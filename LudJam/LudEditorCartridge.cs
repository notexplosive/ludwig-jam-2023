using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ExplogineCore;
using ExplogineCore.Data;
using ExplogineMonoGame;
using ExplogineMonoGame.Cartridges;
using ExplogineMonoGame.Data;
using ExplogineMonoGame.Gui;
using ExplogineMonoGame.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LudJam;

public class LudEditorCartridge : NoProviderCartridge
{
    public const int TextureFrameSize = 511;
    public const int BrickScalar = 8;
    public const int BackgroundScalar = 4;
    private int _currentToolIndex;
    private Action<string>? _promptCallback;
    private string _promptText = string.Empty;
    private EditorState _state = new();
    private TextInputWidget _textField = null!;
    private IGuiTheme _theme = null!;
    private List<IEditorTool> _tools = new();

    public LudEditorCartridge(IRuntime runtime) : base(runtime)
    {
    }

    public IFileSystem EditorDevelopmentFileSystem
    {
        get
        {
#if DEBUG
            return new RealFileSystem(Path.Join(
                AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Assets", "Content",
                "cat"));
            #else
            return Runtime.FileSystem.Local;
#endif
            
        }
    }

    public override CartridgeConfig CartridgeConfig => new(new Point(1920, 1080), SamplerState.PointWrap);

    public IEditorTool CurrentTool => _tools[_currentToolIndex];

    public override void OnCartridgeStarted()
    {
        var bigFont = Client.Assets.GetFont("engine/logo-font", 72);
        _theme = new SimpleGuiTheme(Color.White, Color.Black, Color.Transparent, bigFont);
        _textField = new TextInputWidget(
            Runtime.Window.RenderResolution.ToRectangleF().InflatedMaintainAspectRatio(-300),
            bigFont,
            new TextInputWidget.Settings {Depth = Depth.Front, IsSingleLine = true, ShowScrollbar = false});
        _tools = new List<IEditorTool>
        {
            new SelectionTool(),
            new WallTool(),
            new PlaceCatTool(),
            new PlaceSpawnTool()
        };
    }

    public override void Draw(Painter painter)
    {
        _textField.PrepareDraw(painter, _theme);

        // Draw background
        painter.BeginSpriteBatch();
        var backgroundColor = ColorExtensions.FromRgbHex(0x333333);
        var backgroundAccentColor = ColorExtensions.FromRgbHex(0x222222);
        var renderResolution = Runtime.Window.RenderResolution.ToRectangleF();
        painter.DrawRectangle(renderResolution, new DrawSettings {Color = backgroundColor});
        painter.DrawAsRectangle(Client.Assets.GetTexture("Background"), renderResolution,
            new DrawSettings
            {
                SourceRectangle = new Rectangle((_state.Camera.TopLeftPosition / 2f).ToPoint(),
                    (renderResolution.Size * LudEditorCartridge.BackgroundScalar).ToPoint()),
                Color = backgroundAccentColor
            });
        painter.EndSpriteBatch();

        // Draw Level
        painter.BeginSpriteBatch(_state.Camera.CanvasToScreen);
        _state.Level.Draw(painter);
        painter.EndSpriteBatch();

        painter.BeginSpriteBatch(_state.Camera.CanvasToScreen);
        CurrentTool.Draw(painter);
        painter.EndSpriteBatch();

        // Draw Editor Status
        painter.BeginSpriteBatch();
        var statusText = new StringBuilder();
        for (var i = 0; i < _tools.Count; i++)
        {
            var tool = _tools[i];
            statusText.Append(tool == CurrentTool ? "[" : " ");
            statusText.Append($"{tool.Name} ({i + 1})");
            statusText.Append(tool == CurrentTool ? "]" : " ");
            statusText.Append(" ");
        }

        painter.DrawDebugStringAtPosition(statusText.ToString(), new Vector2(0), new DrawSettings());
        painter.EndSpriteBatch();

        if (_state.CurrentMode == Mode.Typing)
        {
            painter.BeginSpriteBatch();
            var textRect = _textField.OutputRectangle;
            var promptPosition = textRect.Location - new Vector2(0, 32);
            var promptFont = Client.Assets.GetFont("engine/logo-font", 32);
            painter.DrawStringAtPosition(promptFont, _promptText, promptPosition, new DrawSettings());
            painter.DrawRectangle(Runtime.Window.RenderResolution.ToRectangleF(),
                new DrawSettings {Color = Color.Black.WithMultipliedOpacity(0.25f), Depth = Depth.Back});
            painter.DrawStringAtPosition(promptFont, "ENTER to submit; ESC to cancel",
                textRect.BottomLeft + new Vector2(0, 32), new DrawSettings());
            _textField.Draw(painter);
            painter.EndSpriteBatch();
        }
    }

    public override void Update(float dt)
    {
        _state.Level.Update(dt);
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
        if (_state.CurrentMode == Mode.Main)
        {
            var pressedNumber = PressedNumberRowButton(input);
            if (pressedNumber != null)
            {
                _currentToolIndex = pressedNumber.Value % _tools.Count;
            }

            var cameraSpaceLayer = hitTestStack.AddLayer(_state.Camera.ScreenToCanvas, Depth.Middle);

            if (input.Mouse.GetButton(MouseButton.Middle).IsDown)
            {
                var delta = input.Mouse.Delta(cameraSpaceLayer.WorldMatrix);
                _state.Camera.CenterPosition -= delta;
            }

            if (input.Mouse.ScrollDelta() != 0)
            {
                if (input.Mouse.ScrollDelta() > 0)
                {
                    _state.Camera.ZoomInTowards(10, input.Mouse.Position(cameraSpaceLayer.WorldMatrix));
                }
                else
                {
                    _state.Camera.ZoomOutFrom(10, input.Mouse.Position(cameraSpaceLayer.WorldMatrix));
                }
            }

            _state.Level.UpdateInput(input, cameraSpaceLayer);

            var isWithinScreen = Runtime.Window.RenderResolution.ToRectangleF().Contains(input.Mouse.Position());
            CurrentTool.UpdateInput(input, cameraSpaceLayer, _state.Level, isWithinScreen);

            HotKeys.RunBinding(input, HotKeys.Ctrl, Keys.N, NewFile);
            HotKeys.RunBinding(input, HotKeys.Ctrl, Keys.S, Save);
        }
        else if (_state.CurrentMode == Mode.Typing)
        {
            _textField.UpdateInput(input, hitTestStack);

            HotKeys.RunBinding(input, HotKeys.NoModifiers, Keys.Escape, UnPrompt);
            HotKeys.RunBinding(input, HotKeys.NoModifiers, Keys.Enter, SubmitPrompt);
        }
    }

    private void SubmitPrompt()
    {
        _state.CurrentMode = Mode.Main;
        _promptCallback?.Invoke(_textField.Text);
    }

    private void UnPrompt()
    {
        _state.CurrentMode = Mode.Main;
        _promptCallback = null;
    }

    private void Save()
    {
        if (_state.SavedName == null)
        {
            PromptForName();
        }
        else
        {
            SaveAs(_state.SavedName);
        }
    }

    private void SaveAs(string stateSavedName)
    {
        _state.SavedName = stateSavedName;
        var fileName = $"{stateSavedName}.json";

        var content = new Blob();
        foreach (var actor in _state.Level.Scene.AllActors())
        {
            var serializable = actor.GetComponent<EditorSerializable>();
            if (serializable != null)
            {
                content.Add(serializable.Serialize());
            }
        }

        Client.Debug.Log($"Writing file {fileName}");
        EditorDevelopmentFileSystem.WriteToFile(fileName, content.AsJson());
        Runtime.FileSystem.Local.WriteToFile(fileName, content.AsJson());
    }

    private void PromptForName()
    {
        _promptText = "Please name your level:";
        _state.CurrentMode = Mode.Typing;
        _promptCallback = str => { SaveAs(str); };
    }

    public void NewFile()
    {
        _state = new EditorState();
    }

    private int? PressedNumberRowButton(ConsumableInput input)
    {
        var keys = new[]
        {
            Keys.D1,
            Keys.D2,
            Keys.D3,
            Keys.D4,
            Keys.D5,
            Keys.D6,
            Keys.D7,
            Keys.D8,
            Keys.D9,
            Keys.D0
        };

        for (var i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            if (input.Keyboard.GetButton(key).WasPressed)
            {
                return i;
            }
        }

        return null;
    }
}
