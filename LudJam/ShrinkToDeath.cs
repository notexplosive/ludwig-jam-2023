using ExplogineMonoGame;
using ExplogineMonoGame.Data;
using Fenestra.Components;
using FenestraSceneGraph;

namespace LudJam;

public class ShrinkToDeath : BaseComponent
{
    private float _duration;

    public ShrinkToDeath(Actor actor) : base(actor)
    {
    }
    
    public void Init(float duration)
    {
        _duration = duration;
    }

    public override void Update(float dt)
    {
        Actor.Scale -= new Scale2D(0.01f);
        
        if (_duration < 0 || Actor.Scale.Value.MinXy() < 0)
        {
            Actor.DestroyDeferred();
        }
    }

    public override void Draw(Painter painter)
    {
        
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
        
    }
}
