using ExplogineMonoGame;
using ExplogineMonoGame.Data;
using Fenestra.Components;
using FenestraSceneGraph;

namespace LudJam;

public class CameraFocusPoint : BaseComponent
{
    public CameraFocusPoint(Actor actor) : base(actor)
    {
    }

    public override void Update(float dt)
    {
        LudGameCartridge.Instance.AddCameraFocusPoint(Actor.Position);
    }

    public override void Draw(Painter painter)
    {
        
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
        
    }
}
