using System;
using ExplogineCore.Data;
using ExplogineMonoGame;
using ExplogineMonoGame.AssetManagement;
using ExplogineMonoGame.Data;
using ExplogineMonoGame.Input;
using ExTween;
using Fenestra.Components;
using FenestraSceneGraph;
using FenestraSceneGraph.Components;
using Microsoft.Xna.Framework;

namespace LudJam;

public class PlayerMovement : BaseComponent
{
    private readonly BoundingRectangle _boundingRect;
    private readonly Drag<Vector2> _drag;
    private readonly TweenableFloat _flameHeight = new();
    private readonly SequenceTween _flameTween = new();
    private readonly SimplePhysics _physics;
    private readonly SpriteFrameRenderer _spriteFrameRenderer;
    private float _elapsedTime;
    private float _mostRecentMovedAngle;

    public PlayerMovement(Actor actor) : base(actor)
    {
        _boundingRect = RequireComponent<BoundingRectangle>();
        _physics = RequireComponent<SimplePhysics>();
        _spriteFrameRenderer = RequireComponent<SpriteFrameRenderer>();
        _drag = new Drag<Vector2>();
        _physics.IsGravityEnabled = false;
        _physics.TimeScale = 3;
    }

    public bool IsDraggingAtAll => _drag.IsDragging;
    public bool IsMeaningfullyDragging => IsDraggingAtAll && _drag.TotalDelta.Length() > 5;
    public Vector2 DragDelta => _drag.TotalDelta;
    public Vector2 JumpImpulseVelocity => -DragDelta * 2f;

    public override void Update(float dt)
    {
        _flameTween.Update(dt);
        _elapsedTime += dt;
        if (!_physics.IsFrozen)
        {
            foreach (var solid in Actor.Scene.GetAllComponentsMatching<Solid>())
            {
                var otherRect = solid.Rectangle;
                var myRect = _boundingRect.Rectangle;
                if (solid.Rectangle.Intersects(_boundingRect))
                {
                    // figure out which side we hit in this awful (but extremely cheap) way
                    var newVelocity = _physics.Velocity;
                    var topDifference = Math.Abs(otherRect.Top - myRect.Bottom);
                    var bottomDifference = Math.Abs(otherRect.Bottom - myRect.Top);
                    var leftDifference = Math.Abs(otherRect.Left - myRect.Right);
                    var rightDifference = Math.Abs(otherRect.Right - myRect.Left);
                    var minTopBottom = Math.Min(topDifference, bottomDifference);
                    var minLeftRight = Math.Min(leftDifference, rightDifference);
                    var totalMin = Math.Min(minTopBottom, minLeftRight);

                    // ReSharper disable CompareOfFloatsByEqualityOperator
                    if (totalMin == minTopBottom)
                    {
                        newVelocity.Y = -newVelocity.Y;
                    }

                    if (totalMin == minLeftRight)
                    {
                        newVelocity.X = -newVelocity.X;
                    }
                    // ReSharper restore CompareOfFloatsByEqualityOperator

                    var scene = Actor.Scene;
                    Actor.DestroyDeferred();
                    Actor.Scene.AddDeferredAction(() =>
                    {
                        var debris = scene.AddNewActor();
                        debris.Angle = Actor.Angle;
                        debris.Position = Actor.Position;
                        debris.Depth = Depth.Front + 10;
                        debris.Scale = Actor.Scale;

                        var phys = debris.AddComponent<SimplePhysics>().Init(newVelocity / 2f);
                        phys.TimeScale = _physics.TimeScale;
                        debris.AddComponent<SpriteFrameRenderer>()
                            .Init(Client.Assets.GetAsset<SpriteSheet>("Sheet"), 8);
                        debris.AddComponent<RandomSpin>().Init(newVelocity.Normalized().X / 50f);
                    });
                }
            }
        }
    }

    public override void Draw(Painter painter)
    {
        if (IsDraggingAtAll)
        {
            _spriteFrameRenderer.Frame = 3;

            var angle = DragDelta.GetAngleFromUnitX();

            if (IsMeaningfullyDragging)
            {
                Actor.Angle = angle + MathF.PI;
                painter.DrawLine(_drag.StartingValue, _drag.StartingValue + _drag.TotalDelta,
                    new LineDrawSettings {Thickness = 2});
            }

            // predict arc
            var velocity = CalculateVelocityAfterJump();
            var position = Actor.Position;
            var dt = 1 / 60f;

            for (var i = 0; i < 512; i++)
            {
                var previousPosition = position;
                position += velocity * dt;
                velocity += SimplePhysics.Gravity * dt;

                var lineIsOutsidePlayer = (position - Actor.Position).Length() > 60;
                if (lineIsOutsidePlayer && i % 2 == 0)
                {
                    painter.DrawLine(previousPosition, position, new LineDrawSettings {Thickness = 3});
                }
            }
        }

        if (!IsDraggingAtAll)
        {
            Actor.Angle = _physics.Velocity.GetAngleFromUnitX();
            _mostRecentMovedAngle = Actor.Angle;
            _spriteFrameRenderer.Frame = 4;
        }

        // draw flame
        var scale = Actor.Scale.Value;
        var polar = Vector2Extensions.Polar(0, _mostRecentMovedAngle);
        Client.Assets.GetAsset<SpriteSheet>("Sheet").DrawFrameAtPosition(painter, 2, Actor.Position - polar,
            new Scale2D(new Vector2(scale.X * 0.8f, scale.Y * _flameHeight)),
            new DrawSettings
            {
                Origin = new DrawOrigin(new Vector2(LudEditorCartridge.TextureFrameSize / 2f,
                    LudEditorCartridge.TextureFrameSize)),
                Angle = _mostRecentMovedAngle - MathF.PI / 2f,
                Depth = Actor.Depth + 1, Color = Color.OrangeRed,
                Flip = new XyBool(MathF.Sin(_elapsedTime * 30) > 0, false)
            });
    }

    private Vector2 CalculateVelocityAfterJump()
    {
        return JumpImpulseVelocity;
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
        if (input.Mouse.GetButton(MouseButton.Left).WasPressed)
        {
            _physics.RaiseFreezeSemaphore();
            _drag.Start(input.Mouse.Position(hitTestStack.WorldMatrix));
        }

        _drag.AddDelta(input.Mouse.Delta(hitTestStack.WorldMatrix));

        if (input.Mouse.GetButton(MouseButton.Left).WasReleased)
        {
            var maxHeight = JumpImpulseVelocity.Length() / 550;
            var duration = JumpImpulseVelocity.Length() / 3000;
            _flameTween.Clear();
            _flameTween
                .Add(new Tween<float>(_flameHeight, maxHeight, duration / 4f, Ease.SineFastSlow))
                .Add(new Tween<float>(_flameHeight, 0, duration, Ease.SineFastSlow))
                ;
            _physics.IsGravityEnabled = true;
            _physics.LowerFreezeSemaphore();
            Jump(JumpImpulseVelocity);
            _drag.End();
        }
    }

    private void Jump(Vector2 impulse)
    {
        _physics.Velocity = CalculateVelocityAfterJump();
    }
}
