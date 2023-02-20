#nullable enable
using System;
using ExplogineMonoGame;
using ExplogineMonoGame.Data;
using ExplogineMonoGame.Rails;
using FenestraSceneGraph;

namespace Fenestra.Components;

public abstract class BaseComponent : IDrawHook, IUpdateInputHook
{
    public BaseComponent(Actor actor)
    {
        Actor = actor;
    }

    public Actor Actor { get; }
    
    protected T RequireComponent<T>() where T : BaseComponent
    {
        var component = Actor.GetComponent<T>();
        if (component != null)
        {
            return component;
        }
        else
        {
            throw new Exception($"{GetType().Name} requires {typeof(T).Name}");
        }
    }

    public virtual void PrepareDraw(Painter painter)
    {
        // This method is intentionally left blank
    }
    public abstract void Draw(Painter painter);
    public abstract void UpdateInput(ConsumableInput input, HitTestStack hitTestStack);
    public virtual void Update(float dt)
    {
        // This method is intentionally left blank
    }

    public static TComponent InstantiateComponent<TComponent>(Actor actor) where TComponent : BaseComponent
    {
        var component = (TComponent?) Activator.CreateInstance(typeof(TComponent), actor);
        return component ??
               throw new Exception(
                   $"Activator could not create instance of {typeof(TComponent).Name} using `new {typeof(TComponent).Name}({nameof(BaseComponent.Actor)}),` maybe this constructor isn't supported?");
    }

    public virtual void ReceiveBroadcast(ISceneMessage message)
    {
        // this method is intentionally left blank
    }
}
