using ExplogineMonoGame;
using ExplogineMonoGame.Data;
using Fenestra;
using Fenestra.Components;
using Microsoft.Xna.Framework;

namespace FenestraSceneGraph.Components;

public class SimplePhysics : BaseComponent
{
    public static readonly Vector2 Gravity = new(0, 512);
    private int _freezeSemaphore;
    private float _freezeTimer;

    public SimplePhysics(Actor actor) : base(actor)
    {
    }

    public Vector2 Velocity { get; set; }
    public bool IsGravityEnabled { get; set; } = true;
    public bool IsFrozen => _freezeTimer > 0 || _freezeSemaphore > 0;

    public void Init(Vector2 startingVelocity)
    {
        Velocity = startingVelocity;
    }

    public void RaiseFreezeSemaphore()
    {
        _freezeSemaphore++;
    }

    public void LowerFreezeSemaphore()
    {
        _freezeSemaphore--;

        if (_freezeSemaphore < 0)
        {
            _freezeSemaphore = 0;
        }
    }

    public override void ReceiveBroadcast(ISceneMessage message)
    {
        if (message is HitStunFreezeMessage freeze)
        {
            _freezeTimer = freeze.Duration;
        }
    }

    public override void Draw(Painter painter)
    {
        if (Client.Debug.IsActive)
        {
            painter.DrawLine(Actor.Position, Actor.Position + Velocity,
                new LineDrawSettings {Thickness = 5, Color = Color.Orange, Depth = Actor.Depth - 1});
        }
    }

    public override void Update(float dt)
    {
        if (IsFrozen)
        {
            _freezeTimer -= dt;
            return;
        }

        // apply velocity
        Actor.Position += Velocity * dt;

        if (IsGravityEnabled)
        {
            Velocity += SimplePhysics.Gravity * dt;
        }
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
    }
}
