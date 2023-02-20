using System.Collections.Generic;
using System.Text;
using ExplogineCore.Data;
using ExplogineMonoGame;
using ExplogineMonoGame.Cartridges;
using ExplogineMonoGame.Data;
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
    private readonly EditorState _state = new();
    private List<IEditorTool> _tools = new();
    private int _currentToolIndex;

    public LudEditorCartridge(IRuntime runtime) : base(runtime)
    {
    }

    public override CartridgeConfig CartridgeConfig => new(new Point(1920, 1080), SamplerState.PointWrap);

    public IEditorTool CurrentTool => _tools[_currentToolIndex];

    public override void OnCartridgeStarted()
    {
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
    }

    public override void Update(float dt)
    {
        _state.Level.Update(dt);
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
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
        CurrentTool.UpdateInput(input, cameraSpaceLayer, _state.Level);
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