using ExplogineCore.Data;
using ExplogineMonoGame;
using ExplogineMonoGame.Data;
using Fenestra.Components;
using Microsoft.Xna.Framework;

namespace FenestraSceneGraph.Components;

public class BoundingRectangle : BaseComponent
{
    private RectangleF _relativeRectangle;

    public BoundingRectangle(Actor actor) : base(actor)
    {
    }

    public RectangleF Rectangle => _relativeRectangle.Moved(Actor.Position);

    public BoundingRectangle Init(Vector2 size, DrawOrigin? drawOrigin = null)
    {
        drawOrigin ??= DrawOrigin.Zero;
        _relativeRectangle = new RectangleF(-drawOrigin.Value.Calculate(size), size);
        return this;
    }

    public static implicit operator RectangleF(BoundingRectangle me)
    {
        return me.Rectangle;
    }

    public override void Draw(Painter painter)
    {
        if (Client.Debug.IsActive)
        {
            painter.DrawLineRectangle(Rectangle, new LineDrawSettings {Color = Color.Blue, Depth = Depth.Front});
        }
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
    }
}
