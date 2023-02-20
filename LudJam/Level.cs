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
    private readonly Scene _scene;
    private Actor? _cat;
    private Actor? _spawn;

    public Level()
    {
        _scene = new Scene(new Point(1920, 1080));
    }

    public void AddWall(Rectangle wallRectangle)
    {
        _scene.AddDeferredAction(() =>
        {
            var actor = _scene.AddNewActor();
            actor.Position = wallRectangle.Location.ToVector2();
            actor.Depth = Depth.Middle + _scene.AllActors().Count();
            actor.AddComponent<BoundingRectangle>().Init(wallRectangle.Size.ToVector2());
            actor.AddComponent<WallRenderer>();
            actor.AddComponent<EditorSerializable>().Init("Wall");
        });
    }

    public void Draw(Painter painter)
    {
        _scene.DrawContent(painter);
    }

    public void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
        _scene.UpdateInput(input, hitTestStack);
    }

    public void Update(float dt)
    {
        _scene.Update(dt);
    }

    public void SetCatPosition(Vector2 position)
    {
        if (_cat == null)
        {
            _scene.AddDeferredAction(() =>
            {
                _cat = _scene.AddNewActor();
                _cat.Scale = LudGameCartridge.ActorScale;
                _cat.AddComponent<EditorSerializable>().Init("Cat");
                _cat.AddComponent<SpriteFrameRenderer>().Init(Client.Assets.GetAsset<SpriteSheet>("Sheet"), 9);
            });
        }

        _scene.AddDeferredAction(() => { _cat!.Position = position; });
    }

    public void SetSpawnPosition(Vector2 position)
    {
        if (_spawn == null)
        {
            _scene.AddDeferredAction(() =>
            {
                _spawn = _scene.AddNewActor();
                _spawn.Scale = LudGameCartridge.ActorScale;
                _spawn.AddComponent<EditorSerializable>().Init("Spawn");
                _spawn.AddComponent<SpriteFrameRenderer>().Init(Client.Assets.GetAsset<SpriteSheet>("Sheet"), 3);
            });
        }

        _scene.AddDeferredAction(() => { _spawn!.Position = position; });
    }
}