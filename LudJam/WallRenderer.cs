using ExplogineMonoGame;
using ExplogineMonoGame.Data;
using Fenestra.Components;
using FenestraSceneGraph;
using FenestraSceneGraph.Components;
using Microsoft.Xna.Framework;

namespace LudJam;

public class WallRenderer : BaseComponent
{
    private readonly BoundingRectangle _boundingRectangle;
    private Color _accentColor;
    private Color _solidColor;

    public WallRenderer(Actor actor) : base(actor)
    {
        _boundingRectangle = RequireComponent<BoundingRectangle>();
    }

    public WallRenderer Init(Color mainColor, Color accentColor)
    {
        _solidColor = mainColor;
        _accentColor = accentColor;
        return this;
    }

    public override void Draw(Painter painter)
    {
        painter.DrawRectangle(_boundingRectangle, new DrawSettings {Color = _solidColor, Depth = Actor.Depth});

        var insetRectangle = _boundingRectangle.Rectangle;
        insetRectangle.Inflate(-2, -2);

        painter.DrawAsRectangle(Client.Assets.GetTexture("Brick"), insetRectangle,
            new DrawSettings
            {
                Depth = Actor.Depth - 1,
                SourceRectangle = new Rectangle(Point.Zero,
                    (insetRectangle.Size * LudEditorCartridge.BrickScalar).ToPoint()),
                Color = _accentColor
            });
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
    }
}
