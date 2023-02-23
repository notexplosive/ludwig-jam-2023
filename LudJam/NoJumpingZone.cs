using ExplogineCore.Data;
using ExplogineMonoGame;
using ExplogineMonoGame.Data;
using Fenestra.Components;
using FenestraSceneGraph;
using FenestraSceneGraph.Components;
using Microsoft.Xna.Framework;

namespace LudJam;

public class NoJumpingZone : BaseComponent
{
    private readonly BoundingRectangle _boundingRectangle;

    public NoJumpingZone(Actor actor) : base(actor)
    {
        _boundingRectangle = RequireComponent<BoundingRectangle>();
    }

    public RectangleF Rectangle => _boundingRectangle.Rectangle;

    public override void Draw(Painter painter)
    {
        painter.DrawRectangle(Rectangle,new DrawSettings{Color = Color.Purple.WithMultipliedOpacity(0.5f), Depth = Depth.Front});
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
        
    }
}
