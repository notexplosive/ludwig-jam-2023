using System.Collections.Generic;
using System.Linq;
using ExplogineCore.Data;
using ExplogineMonoGame;
using ExplogineMonoGame.AssetManagement;
using ExplogineMonoGame.Data;
using Fenestra;
using FenestraSceneGraph;
using FenestraSceneGraph.Components;
using Microsoft.Xna.Framework;

namespace LudJam;

public class Level
{
    private Actor? _cat;
    private Actor? _spawn;

    public Level()
    {
        Scene = new Scene(new Point(1920, 1080));
        Scene.RemovedActor += WhenActorRemoved;
    }

    private void WhenActorRemoved(Actor actor)
    {
        HoverStates.Remove(actor);
    }

    public Scene Scene { get; }
    public Dictionary<Actor, HoverState> HoverStates { get; set; } = new();

    public void AddWall(Rectangle wallRectangle)
    {
        Scene.AddDeferredAction(() =>
        {
            var actor = Scene.AddNewActor();
            actor.Position = wallRectangle.Location.ToVector2();
            actor.Depth = Depth.Middle + Scene.AllActors().Count() * 5;
            actor.AddComponent<BoundingRectangle>().Init(wallRectangle.Size.ToVector2());
            actor.AddComponent<WallRenderer>();
            actor.AddComponent<EditorSerializable>().Init("Wall");
        });
    }

    public void Draw(Painter painter)
    {
        Scene.DrawContent(painter);
    }

    public void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
        Scene.UpdateInput(input, hitTestStack);
    }

    public void Update(float dt)
    {
        Scene.Update(dt);
    }

    public void SetCatPosition(Vector2 position)
    {
        if (_cat == null)
        {
            Scene.AddDeferredAction(() =>
            {
                _cat = Scene.AddNewActor();
                _cat.Scale = LudGameCartridge.ActorScale;
                _cat.Depth = Depth.Front + 100;
                _cat.AddComponent<EditorSerializable>().Init("Cat");
                _cat.AddComponent<SpriteFrameRenderer>().Init(Client.Assets.GetAsset<SpriteSheet>("Sheet"), 9);
            });
        }

        Scene.AddDeferredAction(() => { _cat!.Position = position; });
    }

    public void SetSpawnPosition(Vector2 position)
    {
        if (_spawn == null)
        {
            Scene.AddDeferredAction(() =>
            {
                _spawn = Scene.AddNewActor();
                _spawn.Scale = LudGameCartridge.ActorScale;
                _spawn.Depth = Depth.Front + 100;
                _spawn.AddComponent<EditorSerializable>().Init("Spawn");
                _spawn.AddComponent<SpriteFrameRenderer>().Init(Client.Assets.GetAsset<SpriteSheet>("Sheet"), 3);
            });
        }

        Scene.AddDeferredAction(() => { _spawn!.Position = position; });
    }
}
