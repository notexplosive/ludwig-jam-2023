using System;
using ExplogineCore.Data;
using ExplogineMonoGame;
using ExplogineMonoGame.AssetManagement;
using ExplogineMonoGame.Data;
using FenestraSceneGraph;
using FenestraSceneGraph.Components;
using Microsoft.Xna.Framework;

namespace Fenestra.Components;

public class SpriteRenderer : BaseComponent
{
    private IFrameAnimation _currentAnimation = null!;
    private float _elapsedTime;

    public SpriteRenderer(Actor actor) : base(actor)
    {
        Color = Color.White;
    }

    public DrawOrigin Origin { get; set; } = DrawOrigin.Center;
    public SpriteSheet SpriteSheet { get; set; } = null!;
    public Color Color { get; set; }
    public int FramesPerSecond { get; set; } = 15;
    public bool IsPaused { get; set; }
    public XyBool Flip { get; set; } = XyBool.False;
    public int CurrentFrame => _currentAnimation.GetFrame(_elapsedTime);
    public bool Visible { get; set; } = true;

    public SpriteRenderer Init(SpriteSheet spriteSheet)
    {
        SpriteSheet = spriteSheet;
        _currentAnimation = spriteSheet.DefaultAnimation;
        return this;
    }

    public override void Draw(Painter painter)
    {
        if (!Visible)
        {
            return;
        }
        
        SpriteSheet.DrawFrameAtPosition(painter, CurrentFrame, Actor.Position,
            Actor.Scale, new DrawSettings
            {
                Depth = Actor.Depth,
                Color = Color,
                Flip = Flip,
                Angle = Actor.Angle,
                Origin = Origin
            });
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
    }

    public override void Update(float dt)
    {
        if (!IsPaused)
        {
            IncrementTime(dt);
        }
    }

    public SpriteRenderer SetupBox()
    {
        var box = Actor.GetComponent<BoundingRectangle>();
        var gridBasedSpriteSheet = SpriteSheet as GridBasedSpriteSheet;

        if (gridBasedSpriteSheet == null)
        {
            throw new Exception(
                $"{nameof(SpriteRenderer.SetupBox)}() failed because {nameof(SpriteRenderer.SpriteSheet)} was not a {nameof(GridBasedSpriteSheet)}");
        }

        var size = new Vector2
        {
            X = (int) (gridBasedSpriteSheet.FrameSize.X * Actor.Scale.Value.X),
            Y = (int) (gridBasedSpriteSheet.FrameSize.Y * Actor.Scale.Value.Y)
        };

        Actor.AddComponent<BoundingRectangle>().Init(size, DrawOrigin.Center);
        return this;
    }

    public void SetFrame(int frame)
    {
        _elapsedTime = frame;
    }

    public bool IsAnimationFinished()
    {
        return _elapsedTime > _currentAnimation.Length;
    }

    public SpriteRenderer SetAnimation(IFrameAnimation animation)
    {
        if (!_currentAnimation.Equals(animation))
        {
            _elapsedTime = 0;
            _currentAnimation = animation;
        }

        return this;
    }

    private void IncrementTime(float dt)
    {
        SetElapsedTime(_elapsedTime + dt * FramesPerSecond);
    }

    private void SetElapsedTime(float newTime)
    {
        _elapsedTime = newTime;
    }
}
