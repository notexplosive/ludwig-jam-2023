using System;
using System.Collections.Generic;
using ExplogineCore;
using ExplogineCore.Data;
using ExplogineMonoGame;
using ExplogineMonoGame.AssetManagement;
using ExplogineMonoGame.Cartridges;
using ExplogineMonoGame.Data;
using Fenestra;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LudJam;

public class LudGameCartridge : NoProviderCartridge, ILoadEventProvider
{
    private Level _currentLevel = null!;
    private Camera _camera = new Camera(new Vector2(1920, 1080));

    public LudGameCartridge(IRuntime runtime) : base(runtime)
    {
    }

    public override CartridgeConfig CartridgeConfig => new(new Point(1920, 1080), SamplerState.AnisotropicWrap);
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
        _currentLevel = LoadLevel(0);
    }

    private Level LoadLevel(int i)
    {
        if (i < LevelSequence.Length)
        {
            var levelName = LevelSequence[i];
            var levelData = G.EditorDevelopmentFileSystem(Runtime).ReadFile($"Content/cat/{levelName}.json");
            return new Level().LoadFromJson(levelData, true);
        }
        else
        {
            throw new Exception("Ran out of levels! ... I need to make an outro screen");
        }
    }

    public override void Draw(Painter painter)
    {
        G.DrawBackground(painter, Runtime.Window.RenderResolution, _camera);
        
        painter.BeginSpriteBatch(_camera.CanvasToScreen);
        _currentLevel.Scene.DrawContent(painter);
        painter.EndSpriteBatch();
    }

    public override void Update(float dt)
    {
        if (G.ImpactTimer > 0)
        {
            G.ImpactTimer -= dt;
            return;
        }
        
        _currentLevel.Scene.Update(dt);
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
        _currentLevel.Scene.UpdateInput(input, hitTestStack);

        if (input.Keyboard.GetButton(Keys.R, true).WasPressed)
        {
            _currentLevel = LoadLevel(0);
        }
    }

    public override void Unload()
    {
    }

    public static Vector2 GetRandomVector(NoiseBasedRng random)
    {
        return random.NextPositiveVector2()
            .StraightMultiply(new Vector2(random.NextSign(), random.NextSign()));
    }
}
