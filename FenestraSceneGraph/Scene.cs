#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using ExplogineMonoGame;
using ExplogineMonoGame.Data;
using Fenestra.Components;
using FenestraSceneGraph;
using Microsoft.Xna.Framework;

namespace Fenestra;

public class Scene
{
    private readonly List<Actor> _actors = new();
    private readonly DeferredActions _deferredActions = new();
    private Point _worldSize;

    protected bool IsDragging;

    public Scene(Point worldSize)
    {
        WorldSize = worldSize;
    }

    /// <summary>
    ///     Size of the world inside the scene.
    ///     This is usually the same as the size of the window, unless the scene is zoomed in or scrollable
    /// </summary>
    public RectangleF InternalWorldBounds => new(Vector2.Zero, WorldSize.ToVector2());

    public virtual Matrix ScreenToCanvas => Matrix.Identity;
    public virtual Matrix CanvasToScreen => Matrix.Identity;
    public virtual Matrix SceneToDesktop => Matrix.Identity;
    public Matrix DesktopToScene => Matrix.Invert(SceneToDesktop);

    public Point WorldSize
    {
        get => _worldSize;
        set
        {
            WorldSizeChanged?.Invoke(value);
            _worldSize = value;
        }
    }

    public event Action<Point>? WorldSizeChanged;

    public void PrepareDraw(Painter painter)
    {
        foreach (var actor in _actors)
        {
            actor.PrepareDraw(painter);
        }
    }

    public void DrawContent(Painter painter)
    {
        foreach (var actor in _actors)
        {
            actor.Draw(painter);
        }
    }

    public void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
        if (IsDragging)
        {
            return;
        }

        foreach (var actor in _actors)
        {
            actor.UpdateInput(input, hitTestStack);
        }
    }

    public void Update(float dt)
    {
        if (IsDragging)
        {
            return;
        }

        foreach (var actor in _actors)
        {
            actor.Update(dt);
        }

        _deferredActions.RunAllAndClear();
    }

    public Actor AddNewActor()
    {
        var actor = new Actor();
        actor.SwapToScene(this);
        return actor;
    }

    public Actor AddExistingActor(Actor actor)
    {
        _actors.Add(actor);
        return actor;
    }

    public void RemoveActor(Actor actor)
    {
        _actors.Remove(actor);
    }

    public void AddDeferredAction(Action deferredAction)
    {
        _deferredActions.Add(deferredAction);
    }

    [Pure]
    public IEnumerable<Actor> AllActors()
    {
        foreach (var actor in _actors)
        {
            yield return actor;
        }
    }

    [Pure]
    public IEnumerable<T> GetAllComponentsMatching<T>() where T : BaseComponent
    {
        foreach (var actor in _actors)
        {
            var component = actor.GetComponent<T>();

            if (component != null)
            {
                yield return component;
            }
        }
    }

    [Pure]
    public int CountAllComponentsInScene<T>() where T : BaseComponent
    {
        var result = 0;
        foreach (var actor in _actors)
        {
            var component = actor.GetComponent<T>();

            if (component != null)
            {
                result++;
            }
        }

        return result;
    }

    public void Broadcast(ISceneMessage message)
    {
        // Terrible messaging system. But that's OK :)
        foreach (var actor in _actors)
        {
            actor.ReceiveBroadcast(message);
        }
    }

    public void DragStarted()
    {
        IsDragging = true;
        Broadcast(new WindowStartedDraggingMessage());
    }

    public void DragFinished()
    {
        IsDragging = false;
        Broadcast(new WindowStoppedDraggingMessage());
    }

    public class EmptyScene : Scene
    {
        public EmptyScene(Actor residentActor) : base(Point.Zero)
        {
            AddExistingActor(residentActor);
        }
    }
}
