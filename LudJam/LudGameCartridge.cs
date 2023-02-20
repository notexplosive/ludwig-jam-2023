using System;
using System.Collections.Generic;
using ExplogineCore;
using ExplogineMonoGame;
using ExplogineMonoGame.AssetManagement;
using ExplogineMonoGame.Cartridges;
using ExplogineMonoGame.Data;
using Fenestra;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LudJam;

public class LudGameCartridge : NoProviderCartridge, ILoadEventProvider
{
    private Scene _scene = new(new Point(1920, 1080));

    public LudGameCartridge(IRuntime runtime) : base(runtime)
    {
    }

    public override CartridgeConfig CartridgeConfig => new(new Point(1920, 1080), SamplerState.PointWrap);
    public static Scale2D ActorScale => new(new Vector2(0.25f, 0.25f));

    public IEnumerable<ILoadEvent> LoadEvents(Painter painter)
    {
        yield return new AssetLoadEvent("Sheet",
            () => new GridBasedSpriteSheet(Client.Assets.GetTexture("cat/sheet"),
                new Point(LudEditorCartridge.TextureFrameSize)));

        yield return new AssetLoadEvent("Brick",
            () => new TextureAsset(Client.Graphics.CropTexture(
                new Rectangle(0, 0, LudEditorCartridge.TextureFrameSize, LudEditorCartridge.TextureFrameSize),
                Client.Assets.GetTexture("cat/sheet"))));

        yield return new AssetLoadEvent("Background",
            () => new TextureAsset(Client.Graphics.CropTexture(
                new Rectangle(LudEditorCartridge.TextureFrameSize, 0, LudEditorCartridge.TextureFrameSize,
                    LudEditorCartridge.TextureFrameSize),
                Client.Assets.GetTexture("cat/sheet"))));

        yield return new VoidLoadEvent("LevelSequence", () =>
        {
            LudGameCartridge.LevelSequence = Runtime.FileSystem.Local.ReadFile("Content/cat/level-sequence.txt").SplitLines();
        });
    }

    public static string[] LevelSequence { get; private set; } = Array.Empty<string>();

    public override void OnCartridgeStarted()
    {
        _scene = new Scene(new Point(1920, 1080));
    }

    public override void Draw(Painter painter)
    {
        painter.BeginSpriteBatch();
        painter.DrawDebugStringAtPosition("Game", new Vector2(0), new DrawSettings());
        _scene.DrawContent(painter);
        painter.EndSpriteBatch();
    }

    public override void Update(float dt)
    {
        _scene.Update(dt);
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
        _scene.UpdateInput(input, hitTestStack);
    }

    public override void Unload()
    {
    }
}
