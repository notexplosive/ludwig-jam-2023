using System;
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LudJam;

public class Level
{
    public enum WallType
    {
        Solid,
        ToggleWallEven,
        ToggleWallOdd,
        NoJumpingZone,
    }

    private Actor? _cat;
    private ParCounter? _parCounter;
    private Actor? _spawn;
    public int StrokeCount { get; private set; }

    public Level()
    {
        Scene = new Scene(new Point(1920, 1080));
        Scene.RemovedActor += WhenActorRemoved;
    }

    public int ParStrokeCount
    {
        get
        {
            if (_parCounter != null)
            {
                return _parCounter.Par;
            }

            return 0;
        }
    }

    public Scene Scene { get; }
    public Dictionary<Actor, HoverState> HoverStates { get; set; } = new();
    public bool IsPassedPar => StrokeCount > ParStrokeCount;
    public bool IsExactlyPar => StrokeCount == ParStrokeCount;
    public bool IsUnderPar => StrokeCount < ParStrokeCount;

    private void WhenActorRemoved(Actor actor)
    {
        HoverStates.Remove(actor);
    }

    public void AddWall(Rectangle wallRectangle, WallType wallType)
    {
        Scene.AddDeferredAction(() =>
        {
            var wall = Scene.AddNewActor();
            wall.Position = wallRectangle.Location.ToVector2();
            wall.Depth = Depth.Middle - Scene.AllActors().Count() * 5;
            wall.AddComponent<BoundingRectangle>().Init(wallRectangle.Size.ToVector2());
            wall.AddComponent<EditorSerializable>().Init(actor =>
            {
                var rect = actor.GetComponent<BoundingRectangle>()!.Rectangle.ToRectangle();
                return new WallData
                    {X = rect.X, Y = rect.Y, Width = rect.Width, Height = rect.Height, WallType = wallType};
            });

            
            
            if (wallType == WallType.Solid)
            {
                wall.AddComponent<Solid>();
                wall.AddComponent<WallRenderer>().Init(G.WallColor1, G.WallColor2);
            }

            if (wallType == WallType.ToggleWallOdd || wallType == WallType.ToggleWallEven)
            {
                wall.AddComponent<ToggleWall>().Init(wallType == WallType.ToggleWallEven);
            }

            if (wallType == WallType.NoJumpingZone)
            {
                wall.AddComponent<NoJumpingZone>();
            }
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

    public void SetCatPosition(Vector2 position, bool isGame)
    {
        if (_cat == null)
        {
            Scene.AddDeferredAction(() =>
            {
                _cat = Scene.AddNewActor();
                _cat.Scale = LudGameCartridge.ActorScale;
                _cat.Depth = Depth.Front + 100;
                _cat.AddComponent<EditorSerializable>()
                    .Init(actor => new CatData {X = actor.Position.X, Y = actor.Position.Y});
                _cat.AddComponent<SpriteFrameRenderer>()
                    .Init(Client.Assets.GetAsset<SpriteSheet>("Sheet"), 9, G.CharacterColor);

                if (isGame)
                {
                    _cat.AddComponent<BoundingRectangle>()
                        .Init(new Vector2(LudEditorCartridge.TextureFrameSize * LudGameCartridge.ActorScale.Value.X),
                            DrawOrigin.Center);
                    _cat.AddComponent<Cat>().Init(this);
                }
            });
        }

        Scene.AddDeferredAction(() => { _cat!.Position = position; });
    }

    public void SetSpawnPosition(Vector2 position, bool isGame)
    {
        if (_spawn == null)
        {
            Scene.AddDeferredAction(() =>
            {
                _spawn = Scene.AddNewActor();
                _spawn.Position = position;
                _spawn.Scale = LudGameCartridge.ActorScale;
                _spawn.Depth = Depth.Front + 100;
                _spawn.AddComponent<EditorSerializable>()
                    .Init(actor => new SpawnData {X = actor.Position.X, Y = actor.Position.Y});
                _spawn.AddComponent<SpriteFrameRenderer>()
                    .Init(Client.Assets.GetAsset<SpriteSheet>("Sheet"), 3, G.CharacterColor);

                if (isGame)
                {
                    var player = Scene.AddNewActor();
                    player.Position = _spawn.Position;
                    player.Scale = LudGameCartridge.ActorScale;
                    player.Depth = _spawn.Depth;
                    player.AddComponent<SpriteFrameRenderer>().Init(Client.Assets.GetAsset<SpriteSheet>("Sheet"), 3,
                        G.CharacterColor);
                    player.AddComponent<BoundingRectangle>()
                        .Init(
                            new Vector2(LudEditorCartridge.TextureFrameSize * LudGameCartridge.ActorScale.Value.X) / 2f,
                            DrawOrigin.Center);
                    player.AddComponent<SimplePhysics>();
                    player.AddComponent<PlayerMovement>().Init(this);

                    _spawn.Visible = false;
                }
            });
        }

        Scene.AddDeferredAction(() => { _spawn!.Position = position; });
    }

    public Level LoadFromJson(string text, bool isGame)
    {
        try
        {
            var content = JsonConvert.DeserializeObject<Dictionary<string, object>>(text);
            var array = content!["Content"] as JArray;
            var data = new List<ISerializedContent>();

            foreach (var token in array!)
            {
                var obj = token as JObject;
                var name = obj!.Property("Name")!.Value.Value<string>();
                var objString = obj.ToString();

                data.Add(Level.GetSerializedData(name, objString));
            }

            foreach (var item in data)
            {
                item.AddToLevel(this, isGame);
            }
        }
        catch (Exception e)
        {
            Client.Debug.LogWarning($"Could not open file: {e}");
        }

        return this;
    }

    private static ISerializedContent GetSerializedData(string? name, string objString)
    {
        switch (name)
        {
            case "Wall":
                return JsonConvert.DeserializeObject<WallData>(objString);

            case "Spawn":
                return JsonConvert.DeserializeObject<SpawnData>(objString);

            case "Cat":
                return JsonConvert.DeserializeObject<CatData>(objString);

            case "Par":
                return JsonConvert.DeserializeObject<ParData>(objString);
        }

        throw new Exception("No data");
    }

    public string ParStatus()
    {
        return $"Par: {StrokeCount} / {ParStrokeCount}";
    }

    public void IncrementStrokeCount()
    {
        StrokeCount++;
    }

    public Level FinishLoadingLevelForGame()
    {
        _parCounter ??= new ParCounter();
        return this;
    }

    public string ToJson()
    {
        var content = new LevelData();
        foreach (var actor in Scene.AllActors())
        {
            var serializable = actor.GetComponent<EditorSerializable>();
            if (serializable != null)
            {
                content.Add(serializable.Serialize());
            }
        }

        if (_parCounter != null)
        {
            var parData = new ParData
            {
                Amount = ParStrokeCount
            };
            content.Add(parData);
        }

        return content.AsJson();
    }

    public void IncreasePar()
    {
        if (_parCounter == null)
        {
            _parCounter = new ParCounter();
        }

        _parCounter.Par++;
    }

    public void DecreasePar()
    {
        if (_parCounter == null)
        {
            _parCounter = new ParCounter();
        }
        else
        {
            _parCounter.Par--;
        }

        if (_parCounter.Par < 0)
        {
            _parCounter.Par = 0;
        }
    }

    private struct ParData : ISerializedContent
    {
        public string Name => "Par";

        public void AddToLevel(Level level, bool isGame)
        {
            for (var i = 0; i < Amount; i++)
            {
                level.IncreasePar();
            }
        }

        public int Amount { get; set; }
    }

    public struct CatData : ISerializedContent
    {
        public string Name => "Cat";

        public void AddToLevel(Level level, bool isGame)
        {
            level.SetCatPosition(new Vector2(X, Y), isGame);
        }

        public float X { get; set; }
        public float Y { get; set; }
    }

    public struct SpawnData : ISerializedContent
    {
        public string Name => "Spawn";

        public void AddToLevel(Level level, bool isGame)
        {
            level.SetSpawnPosition(new Vector2(X, Y), isGame);
        }

        public float X { get; set; }
        public float Y { get; set; }
    }

    public struct WallData : ISerializedContent
    {
        public string Name => "Wall";

        public void AddToLevel(Level level, bool isGame)
        {
            level.AddWall(new Rectangle(X, Y, Width, Height), WallType);
        }

        public int Width { get; set; }
        public int Height { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public WallType WallType { get; set; }
    }
}