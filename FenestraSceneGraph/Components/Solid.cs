using ExplogineMonoGame;
using ExplogineMonoGame.Data;
using Fenestra.Components;

namespace FenestraSceneGraph.Components;

public class Solid : BaseComponent
{
    public Solid(Actor actor) : base(actor)
    {
        Rectangle = RequireComponent<BoundingRectangle>().Rectangle;
    }

    public RectangleF Rectangle { get; set; }

    public override void Draw(Painter painter)
    {
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
    }
}
