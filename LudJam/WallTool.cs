using System;
using ExplogineCore.Data;
using ExplogineMonoGame;
using ExplogineMonoGame.Data;
using ExplogineMonoGame.Input;
using Microsoft.Xna.Framework;

namespace LudJam;

public class WallTool : IEditorTool
{
    private Vector2? _startPosition;
    private Vector2 _currentPosition;

    private readonly Level.WallType _wallType;

    public WallTool(Level.WallType wallType)
    {
        _wallType = wallType;
    }
    public string Name => _wallType.ToString();
    public void UpdateInput(ConsumableInput input, HitTestStack hitTestStack, Level level, bool isWithinScreen)
    {
        if (input.Mouse.GetButton(MouseButton.Left).WasPressed && isWithinScreen)
        {
            _startPosition = input.Mouse.Position(hitTestStack.WorldMatrix);
        }

        _currentPosition =  input.Mouse.Position(hitTestStack.WorldMatrix);

        if (_startPosition.HasValue)
        {
            var targetRect =
                RectangleF.FromCorners(_startPosition.Value, input.Mouse.Position(hitTestStack.WorldMatrix));
            
            if (input.Mouse.GetButton(MouseButton.Left).WasReleased)
            {
                if (Math.Min(targetRect.Width, targetRect.Height) > 32)
                {
                    Create(targetRect, level);
                }
                _startPosition = null;
            }
            
        }
    }

    private void Create(RectangleF targetRect, Level level)
    {
        level.AddWall(targetRect.ToRectangle(), _wallType);
    }

    public void Draw(Painter painter)
    {
        if (_startPosition.HasValue)
        {
            painter.DrawLineRectangle(RectangleF.FromCorners( _startPosition.Value, _currentPosition), new LineDrawSettings{Thickness = 3, Color = Color.Orange, Depth = Depth.Front});
        }
    }
}