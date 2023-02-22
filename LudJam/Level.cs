﻿using System;
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
    private Actor? _cat;
    private Actor? _player;
    private int _strokeCount;
    private ParCounter? _parCounter;

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

    public Level()
    {
        Scene = new Scene(new Point(1920, 1080));
        Scene.RemovedActor += WhenActorRemoved;
    }

    public Scene Scene { get; }
    public Dictionary<Actor, HoverState> HoverStates { get; set; } = new();
    public bool IsPassedPar => _strokeCount > ParStrokeCount;

    private void WhenActorRemoved(Actor actor)
    {
        HoverStates.Remove(actor);
    }

    public void AddWall(Rectangle wallRectangle)
    {
        Scene.AddDeferredAction(() =>
        {
            var wall = Scene.AddNewActor();
            wall.Position = wallRectangle.Location.ToVector2();
            wall.Depth = Depth.Middle - Scene.AllActors().Count() * 5;
            wall.AddComponent<BoundingRectangle>().Init(wallRectangle.Size.ToVector2());
            wall.AddComponent<WallRenderer>();
            wall.AddComponent<EditorSerializable>().Init(actor =>
            {
                var rect = actor.GetComponent<BoundingRectangle>()!.Rectangle.ToRectangle();
                return new WallData
                    {X = rect.X, Y = rect.Y, Width = rect.Width, Height = rect.Height};
            });
            wall.AddComponent<Solid>();
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
                _cat.AddComponent<EditorSerializable>().Init(actor => new CatData {X = actor.Position.X, Y = actor.Position.Y});
                _cat.AddComponent<SpriteFrameRenderer>().Init(Client.Assets.GetAsset<SpriteSheet>("Sheet"), 9, G.CharacterColor);

                if (isGame)
                {
                    _cat.AddComponent<BoundingRectangle>().Init(new Vector2(LudEditorCartridge.TextureFrameSize * LudGameCartridge.ActorScale.Value.X),DrawOrigin.Center);
                    _cat.AddComponent<Cat>();
                }
            });
        }

        Scene.AddDeferredAction(() => { _cat!.Position = position; });
    }

    public void SetSpawnPosition(Vector2 position, bool isGame)
    {
        if (_player == null)
        {
            Scene.AddDeferredAction(() =>
            {
                _player = Scene.AddNewActor();
                _player.Scale = LudGameCartridge.ActorScale;
                _player.Depth = Depth.Front + 100;
                _player.AddComponent<EditorSerializable>().Init(actor => new SpawnData {X = actor.Position.X, Y = actor.Position.Y});
                _player.AddComponent<SpriteFrameRenderer>().Init(Client.Assets.GetAsset<SpriteSheet>("Sheet"), 3, G.CharacterColor);

                if (isGame)
                {
                    _player.AddComponent<BoundingRectangle>().Init(new Vector2(LudEditorCartridge.TextureFrameSize* LudGameCartridge.ActorScale.Value.X) / 2f,DrawOrigin.Center);
                    _player.AddComponent<SimplePhysics>();
                    _player.AddComponent<PlayerMovement>().Init(this);
                }
            });
        }

        Scene.AddDeferredAction(() => { _player!.Position = position; });
    }

    public Level LoadFromJson(string text, bool isGame = false)
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
        }

        throw new Exception("No data");
    }

    public struct CatData : ISerializedContent
    {
        public string Name => "Cat";

        public void AddToLevel(Level level, bool isGame)
        {
            level.SetCatPosition(new Vector2(X,Y), isGame);
        }

        public float X { get; set; }
        public float Y { get; set; }
    }

    public struct SpawnData : ISerializedContent
    {
        public string Name => "Spawn";

        public void AddToLevel(Level level, bool isGame)
        {
            level.SetSpawnPosition(new Vector2(X,Y), isGame);
        }

        public float X { get; set; }
        public float Y { get; set; }
    }

    public struct WallData : ISerializedContent
    {
        public string Name => "Wall";

        public void AddToLevel(Level level, bool isGame)
        {
            level.AddWall(new Rectangle(X, Y, Width, Height));
        }

        public int Width { get; set; }
        public int Height { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }

    public string ParStatus()
    {
        return $"Par: {_strokeCount} / {ParStrokeCount}";
    }

    public void IncrementStrokeCount()
    {
        _strokeCount++;
    }

    public Level FinishLoadingLevelForGame()
    {
        foreach (var parCounter in Scene.GetAllComponentsMatching<ParCounter>())
        {
            _parCounter = parCounter;
        }

        if (_parCounter == null)
        {
            _parCounter = Scene.AddNewActor().AddComponent<ParCounter>();
        }

        return this;
    }
}