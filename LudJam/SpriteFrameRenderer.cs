using ExplogineCore.Data;
using ExplogineMonoGame;
using ExplogineMonoGame.AssetManagement;
using ExplogineMonoGame.Data;
using Fenestra.Components;
using FenestraSceneGraph;
using Microsoft.Xna.Framework;

namespace LudJam;

public class SpriteFrameRenderer : BaseComponent
{
    private SpriteSheet? _sheet;

    public SpriteFrameRenderer(Actor actor) : base(actor)
    {
    }

    public int Frame { get; set; }
    public DrawOrigin Origin { get; set; } = DrawOrigin.Center;
    public Color Color { get; set; } = Color.White;
    public Vector2 Offset { get; set; }

    public SpriteFrameRenderer Init(SpriteSheet spriteSheet, int frame, Color color)
    {
        _sheet = spriteSheet;
        Frame = frame;
        Color = color;
        return this;
    }

    public override void Draw(Painter painter)
    {
        _sheet?.DrawFrameAtPosition(painter, Frame, Actor.Position + Offset, Actor.Scale,
            new DrawSettings
                {Angle = Actor.Angle, Color = Color, Depth = Actor.Depth, Flip = XyBool.False, Origin = Origin});
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
    }
}
