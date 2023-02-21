using ExplogineMonoGame;
using ExplogineMonoGame.Data;
using Fenestra.Components;
using FenestraSceneGraph;

namespace LudJam;

public class RandomSpin : BaseComponent
{
    private float _angularVelocity;

    public RandomSpin(Actor actor) : base(actor)
    {
    }

    public override void Update(float dt)
    {
        Actor.Angle += _angularVelocity;
    }

    public override void Draw(Painter painter)
    {
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
    }

    public void Init(float angularVelocity)
    {
        _angularVelocity = angularVelocity;
    }
}
