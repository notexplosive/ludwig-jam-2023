using System;
using ExplogineCore.Data;
using ExplogineMonoGame;
using ExplogineMonoGame.AssetManagement;
using ExplogineMonoGame.Data;
using ExplogineMonoGame.Input;
using ExTween;
using Fenestra;
using Fenestra.Components;
using FenestraSceneGraph;
using FenestraSceneGraph.Components;
using Microsoft.Xna.Framework;

namespace LudJam;

public class PlayerMovement : BaseComponent
{
    private const int PhysicsTimeScale = 3;
    private readonly BoundingRectangle _boundingRect;
    private readonly Drag<Vector2> _drag;
    private readonly TweenableFloat _flameHeight = new();
    private readonly SequenceTween _flameTween = new();
    private readonly SimplePhysics _physics;
    private readonly SpriteFrameRenderer _spriteFrameRenderer;
    private bool _canJump = true;
    private float _elapsedTime;
    private Level _level = null!;
    private float _mostRecentMovedAngle;
    private Vector2 _mostRecentVelocity;
    private bool _previousCanJump = true;
    private Vector2 _previousDragDelta;
    private float _smokeTimer;

    public PlayerMovement(Actor actor) : base(actor)
    {
        _boundingRect = RequireComponent<BoundingRectangle>();
        _physics = RequireComponent<SimplePhysics>();
        _spriteFrameRenderer = RequireComponent<SpriteFrameRenderer>();
        _drag = new Drag<Vector2>();
        _physics.IsGravityEnabled = false;
        _physics.TimeScale = PlayerMovement.PhysicsTimeScale;
    }

    public bool IsDraggingAtAll => _drag.IsDragging;
    public bool IsMeaningfullyDragging => IsDraggingAtAll && DragDelta.Length() > 5;

    /// <summary>
    ///     this should be the only thing referencing _drag.TotalDrag
    /// </summary>
    public Vector2 DragDelta
    {
        get
        {
            if (_drag.TotalDelta.Length() > 0)
            {
                return _drag.TotalDelta.Normalized() * MathF.Min(MaxDelta, _drag.TotalDelta.Length());
            }

            return Vector2.Zero;
        }
    }

    public float MaxDelta => 500;

    public Vector2 JumpImpulseVelocity => -DragDelta * 2f;

    public override void Update(float dt)
    {
        _canJump = CalculateCanJump();
        if (_canJump != _previousCanJump)
        {
            if (_canJump)
            {
                G.Music.FadeToMain();
                for (int i = 0; i < 20; i++)
                {
                    SpawnSmokeParticle(_physics.Velocity, Color.Purple, 2f);
                }
                G.PlaySoundEffect("cat/leave-goop", new SoundEffectSettings{Volume = 1f});
            }
            else
            {
                G.Music.FadeToLow();
                for (int i = 0; i < 20; i++)
                {
                    SpawnSmokeParticle(-_physics.Velocity, Color.Purple, 2f);
                }
                G.PlaySoundEffect("cat/enter-goop", new SoundEffectSettings{Volume = 1f});
            }
        }

        _previousCanJump = _canJump;
        _physics.TimeScale = PlayerMovement.PhysicsTimeScale * (_canJump ? 1f : 1 / 2f);

        LudGameCartridge.Instance.AddCameraFocusPoint(Actor.Position);
        LudGameCartridge.Instance.AddCameraFocusPoint(Actor.Position + JumpImpulseVelocity);

        _flameTween.Update(dt);
        _elapsedTime += dt;

        if (_previousDragDelta != DragDelta)
        {
            // we don't need to stop then play here because we just play the sound when available
            G.PlaySoundEffect("cat/spark", new SoundEffectSettings {Volume = 0.25f});
        }

        _previousDragDelta = DragDelta;

        if (IsMeaningfullyDragging)
        {
            var pullPercent = DragDelta.Length() / MaxDelta;
            G.Music.SetLowPercent(pullPercent / 4f);
            _spriteFrameRenderer.Offset =
                LudGameCartridge.GetRandomVector(Client.Random.Dirty) * JumpImpulseVelocity / 100;
        }
        else
        {
            _spriteFrameRenderer.Offset = Vector2.Zero;
        }

        if (!_physics.IsFrozen)
        {
            if (_physics.Velocity.Length() > 0)
            {
                _smokeTimer -= dt;

                if (_smokeTimer < 0)
                {
                    for (var i = 0; i < 3; i++)
                    {
                        SpawnSmokeParticle(-_physics.Velocity / 2f, G.JumpParticleColor);
                    }

                    _smokeTimer = 0.05f;
                }
            }

            foreach (var cat in Actor.Scene.GetAllComponentsMatching<Cat>())
            {
                if (cat.Rectangle.Intersects(_boundingRect))
                {
                    cat.AnimateVictory(Actor.Position);
                    Actor.DestroyDeferred();
                    return;
                }

                if ((cat.Actor.Position - Actor.Position).Length() > 20000)
                {
                    SpawnDeadBody(Vector2.Zero);
                    return;
                }
            }

            foreach (var solid in Actor.Scene.GetAllComponentsMatching<Solid>())
            {
                var otherRect = solid.Rectangle;
                var myRect = _boundingRect.Rectangle;

                var intersection = RectangleF.Intersect(myRect, otherRect);
                if (Math.Abs(intersection.Area - myRect.Area) < 0.1f)
                {
                    G.StopThenPlaySound("engine/ouch", new SoundEffectSettings {Volume = 0.5f});
                    G.ImpactFreeze(0.05f);
                    G.Music.FadeToLow(0.1f);
                    SpawnDeadBody(new Vector2(0, 20));
                }
                else if (otherRect.Intersects(myRect))
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

                    G.StopThenPlaySound("cat/pained-cough", new SoundEffectSettings {Volume = 1f});

                    for (var i = 0; i < 25; i++)
                    {
                        SpawnSmokeParticle(newVelocity, G.CharacterColor);
                    }

                    G.ImpactFreeze(0.05f);
                    G.Music.FadeToLow(0.1f);
                    SpawnDeadBody(newVelocity);
                }
            }
        }
    }

    private void SpawnDeadBody(Vector2 newVelocity)
    {
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
                .Init(Client.Assets.GetAsset<SpriteSheet>("Sheet"), 8, G.CharacterColor.DimmedBy(0.2f));
            debris.AddComponent<RandomSpin>().Init(newVelocity.Normalized().X / 50f);
            debris.AddComponent<CameraFocusPoint>();
            LudGameCartridge.Instance.ResetLevelAfterTimer();
        });
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

        if (_level.StrokeCount == 0 && _elapsedTime > 3f)
        {
            painter.DrawStringAtPosition(Client.Assets.GetFont("cat/Font", 64), "Click and Drag to move",
                Actor.Position + new Vector2(0,-200), new DrawSettings{Origin = DrawOrigin.Center});
        }
        
        if (!IsDraggingAtAll)
        {
            Actor.Angle = _physics.Velocity.GetAngleFromUnitX();
            _mostRecentMovedAngle = Actor.Angle;
            _mostRecentVelocity = _physics.Velocity;
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
                Depth = Actor.Depth + 1, Color = G.FlameColor,
                Flip = new XyBool(MathF.Sin(_elapsedTime * 30) > 0, false)
            });
    }

    private Vector2 CalculateVelocityAfterJump()
    {
        // extracted in case we do additive velocity later
        return JumpImpulseVelocity;
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
        if (input.Mouse.GetButton(MouseButton.Left).WasPressed)
        {
            if (_canJump)
            {
                G.StopThenPlaySound("cat/snap", new SoundEffectSettings());
                _physics.RaiseFreezeSemaphore();
                _drag.Start(input.Mouse.Position());
            }
            else
            {
                G.ImpactFreeze(0.05f);
                for (var i = 0; i < 20; i++)
                {
                    SpawnSmokeParticle(LudGameCartridge.GetRandomVector(Client.Random.Dirty) * 500, Color.Purple);
                }

                G.PlaySoundEffect("cat/glass", new SoundEffectSettings());
            }
        }

        _drag.AddDelta(input.Mouse.Delta());

        if (input.Mouse.GetButton(MouseButton.Left).WasReleased && IsDraggingAtAll)
        {
            G.StopThenPlaySound("cat/firework", new SoundEffectSettings());
            G.Music.FadeToMain();
            for (var i = 0; i < 20; i++)
            {
                SpawnSmokeParticle(-JumpImpulseVelocity, G.JumpParticleColor);
            }

            var maxHeight = JumpImpulseVelocity.Length() / 550;
            var duration = JumpImpulseVelocity.Length() / 3000;
            _flameTween.Clear();
            _flameTween
                .Add(new Tween<float>(_flameHeight, maxHeight, duration / 4f, Ease.SineFastSlow))
                .Add(new Tween<float>(_flameHeight, 0, duration, Ease.SineFastSlow))
                ;
            _physics.IsGravityEnabled = true;
            _physics.LowerFreezeSemaphore();

            _level.IncrementStrokeCount();
            Jump();
            _drag.End();
        }
    }

    private bool CalculateCanJump()
    {
        foreach (var noJumpZone in Actor.Scene.GetAllComponentsMatching<NoJumpingZone>())
        {
            if (noJumpZone.Rectangle.Contains(Actor.Position))
            {
                return false;
            }
        }

        return true;
    }

    private void Jump()
    {
        _physics.Velocity = CalculateVelocityAfterJump();
        Actor.Scene.Broadcast(new JumpMessage());
    }

    private void SpawnSmokeParticle(Vector2 trendingDirection, Color color, float sizeScalar = 1f)
    {
        Actor.Scene.AddDeferredAction(() =>
        {
            var particle = Actor.Scene.AddNewActor();
            particle.Scale =
                LudGameCartridge.ActorScale * (Client.Random.Dirty.NextSign() * Client.Random.Dirty.NextFloat() / 2f) +
                new Scale2D(0.25f * sizeScalar);
            particle.Position = Actor.Position + LudGameCartridge.GetRandomVector(Client.Random.Dirty) *
                Client.Random.Dirty.NextFloat() * 20f;

            particle.AddComponent<SpriteFrameRenderer>()
                .Init(Client.Assets.GetAsset<SpriteSheet>("Sheet"), 11, color);

            particle.AddComponent<ShrinkToDeath>().Init(0.5f);
            var randomDirection =
                LudGameCartridge.GetRandomVector(Client.Random.Dirty) * Client.Random.Dirty.NextFloat() *
                trendingDirection.Length() / 2f + trendingDirection;
            var particlePhysics = particle.AddComponent<SimplePhysics>().Init(randomDirection);
            particlePhysics.IsGravityEnabled = false;
        });
    }

    public void Init(Level level)
    {
        _level = level;
    }
}

public class JumpMessage : ISceneMessage
{
}
