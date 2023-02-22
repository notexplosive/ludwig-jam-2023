using System.Text;
using ExplogineCore.Data;
using ExplogineMonoGame;
using ExplogineMonoGame.Data;
using ExTween;
using ExTweenMonoGame;
using Fenestra;
using Fenestra.Components;
using Fenestra.Data;
using FenestraSceneGraph.Components;
using Microsoft.Xna.Framework;

namespace FenestraSceneGraph;

public class Actor
{
    private readonly List<BaseComponent> _components = new();

    public Actor()
    {
        Scene = new Scene.EmptyScene(this);
    }

    public Tag Tags { get; private set; }

    public Depth Depth { get; set; } = Depth.Middle;

    public float Angle
    {
        get => AngleTweenable.Value;
        set => AngleTweenable.Value = value;
    }

    public Vector2 Position
    {
        get => PositionTweenable.Value;
        set => PositionTweenable.Value = value;
    }

    public Scale2D Scale { get; set; } = Scale2D.One;
    public TweenableFloat AngleTweenable { get; } = new();
    public TweenableVector2 PositionTweenable { get; } = new();
    public Scene Scene { get; private set; }

    public DrawSettings DefaultDrawSettings => new() {Angle = Angle, Depth = Depth};

    public RectangleF BodyRect
    {
        get
        {
            var location = Position;
            var size = Vector2.One;

            var boundingRectangle = GetComponent<BoundingRectangle>();

            if (boundingRectangle != null)
            {
                location = boundingRectangle.Rectangle.Location;
                size = boundingRectangle.Rectangle.Size;
            }

            return new RectangleF(location, size);
        }
    }

    public RectangleF DesktopBodyRect
    {
        get
        {
            var bodyRect = BodyRect;
            return new RectangleF(Vector2.Transform(bodyRect.Location, Scene.SceneToDesktop), bodyRect.Size);
        }
    }

    public bool Visible { get; set; } = true;

    public event Action? Destroyed;
    public event Action? Deleted;

    public void PrepareDraw(Painter painter)
    {
        foreach (var component in _components)
        {
            component.PrepareDraw(painter);
        }
    }

    public void Draw(Painter painter)
    {
        if (!Visible)
        {
            return;
        }
        
        foreach (var component in _components)
        {
            component.Draw(painter);
        }
    }

    public void UpdateInput(ConsumableInput input, HitTestStack hitTestLayer)
    {
        foreach (var component in _components)
        {
            component.UpdateInput(input, hitTestLayer);
        }
    }

    public void Update(float dt)
    {
        foreach (var component in _components)
        {
            component.Update(dt);
        }
    }

    public void ReceiveBroadcast(ISceneMessage message)
    {
        foreach (var component in _components)
        {
            component.ReceiveBroadcast(message);
        }
    }

    public TComponent AddComponent<TComponent>() where TComponent : BaseComponent
    {
        var component = BaseComponent.InstantiateComponent<TComponent>(this);
        _components.Add(component);
        return component;
    }

    public T? GetComponent<T>() where T : BaseComponent
    {
        foreach (var component in _components)
        {
            if (component is T typedComponent)
            {
                return typedComponent;
            }
        }

        return null;
    }

    public void SwapToScene(Scene newScene)
    {
        Scene.RemoveActor(this);
        newScene.AddExistingActor(this);
        Scene = newScene;
    }

    public bool HasComponent<T>() where T : BaseComponent
    {
        return GetComponent<T>() != null;
    }

    public void DestroyDeferred()
    {
        Scene.AddDeferredAction(DestroyImmediate);
    }

    public void DestroyImmediate()
    {
        if (Scene is Scene.EmptyScene)
        {
            Client.Debug.LogWarning("Actor was destroyed twice");
        }

        Destroyed?.Invoke();
        Deleted?.Invoke();
        SwapToScene(new Scene.EmptyScene(this));
    }

    public void AddTag(Tag tag)
    {
        Tags |= tag;
    }

    public bool HasTag(Tag tag)
    {
        return (Tags & tag) > 0;
    }

    public void Delete()
    {
        Deleted?.Invoke();
    }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();
        foreach (var component in _components)
        {
            stringBuilder.Append(component);
            stringBuilder.Append(" ");
        }

        return stringBuilder.ToString();
    }
}
