using System.Collections.Generic;
using ExplogineCore.Data;
using ExplogineMonoGame;
using ExplogineMonoGame.Data;
using ExplogineMonoGame.Input;
using FenestraSceneGraph;
using FenestraSceneGraph.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace LudJam;

public class SelectionTool : IEditorTool
{
    private readonly RectResizer _rectResizer = new();
    private Vector2 _moveDelta;
    private Actor? _selectedActor;
    public string Name => "Selection";

    public void UpdateInput(ConsumableInput input, HitTestStack hitTestStack, Level level, bool isWithinScreen)
    {
        var isResizing = false;
        if (_selectedActor != null)
        {
            if (input.Keyboard.GetButton(Keys.Delete).WasPressed)
            {
                var captured = _selectedActor;
                captured.DestroyDeferred();
                _selectedActor = null;
            }
        }

        if (_selectedActor != null)
        {
            var boundingRectangle = _selectedActor.GetComponent<BoundingRectangle>();
            if (boundingRectangle != null)
            {
                var newRect = _rectResizer.GetResizedRect(input, hitTestStack, boundingRectangle, Depth.Front,
                    hitTestStack.WorldMatrix, 10, new Point(20));

                _selectedActor.Position = newRect.TopLeft;
                boundingRectangle.Init(newRect.Size);

                if (_rectResizer.HasGrabbed)
                {
                    isResizing = true;
                }
            }
        }

        if (!isResizing)
        {
            if (isWithinScreen)
            {
                foreach (var actor in level.Scene.AllActors())
                {
                    var boundingRectangle = actor.GetComponent<BoundingRectangle>();
                    var isEditorSerializable = actor.GetComponent<EditorSerializable>() != null;

                    if (boundingRectangle != null && isEditorSerializable)
                    {
                        if (!level.HoverStates.ContainsKey(actor))
                        {
                            level.HoverStates.Add(actor, new HoverState());
                        }

                        hitTestStack.AddZone(boundingRectangle.Rectangle, actor.Depth, level.HoverStates[actor], true);
                    }
                }
            }

            var allHoveredActors = new List<Actor>();
            foreach (var keyValue in level.HoverStates)
            {
                var hoveredActor = keyValue.Key;
                var hoverState = keyValue.Value;
                if (hoverState.IsHovered)
                {
                    allHoveredActors.Add(hoveredActor);
                }
            }

            allHoveredActors.Sort((a, b) => a.Depth.AsInt.CompareTo(b.Depth.AsInt));

            if (input.Mouse.GetButton(MouseButton.Left).WasPressed)
            {
                _moveDelta = Vector2.Zero;

                if (_selectedActor != null)
                {
                    var boundingRectangle = _selectedActor.GetComponent<BoundingRectangle>();
                    if (boundingRectangle != null)
                    {
                        if (!boundingRectangle.Rectangle.Contains(input.Mouse.Position(hitTestStack.WorldMatrix)))
                        {
                            _selectedActor = null;
                        }
                    }
                }
            }

            if (input.Mouse.GetButton(MouseButton.Left).IsDown)
            {
                var delta = input.Mouse.Delta(hitTestStack.WorldMatrix);
                _moveDelta += delta;

                if (_selectedActor != null)
                {
                    _selectedActor.Position += delta;
                }
            }

            if (input.Mouse.GetButton(MouseButton.Left).WasReleased && _moveDelta.Length() < 1 &&
                allHoveredActors.Count > 0)
            {
                if (_selectedActor == null || !allHoveredActors.Contains(_selectedActor))
                {
                    _selectedActor = allHoveredActors[0];
                }
                else
                {
                    var index = allHoveredActors.IndexOf(_selectedActor);
                    var nextIndex = (index + 1) % allHoveredActors.Count;

                    if (nextIndex < allHoveredActors.Count)
                    {
                        _selectedActor = allHoveredActors[nextIndex];
                    }
                }
            }
        }
    }

    public void Draw(Painter painter)
    {
        if (_selectedActor != null)
        {
            var boundingRect = _selectedActor.GetComponent<BoundingRectangle>();
            if (boundingRect != null)
            {
                painter.DrawLineRectangle(boundingRect.Rectangle,
                    new LineDrawSettings {Color = Color.Orange, Thickness = 5});
            }
        }
    }

    public void OnLevelLoad(Level level)
    {
        _selectedActor = null;
    }
}
