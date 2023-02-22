using ExplogineMonoGame;
using ExplogineMonoGame.Data;
using Fenestra.Components;
using FenestraSceneGraph;

namespace LudJam;

public class ParCounter : BaseComponent
{
    public ParCounter(Actor actor) : base(actor)
    {
    }

    public override void Draw(Painter painter)
    {
        
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
        
    }

    public int Par { get; set; }
}
