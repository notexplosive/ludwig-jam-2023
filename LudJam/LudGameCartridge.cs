using System.Collections.Generic;
using ExplogineMonoGame;
using ExplogineMonoGame.AssetManagement;
using ExplogineMonoGame.Cartridges;
using ExplogineMonoGame.Data;
using Fenestra;
using Microsoft.Xna.Framework;

namespace LudJam;

public class LudGameCartridge : NoProviderCartridge, ILoadEventProvider
{
    private Scene _scene = new(new Point(1920, 1080));

    public LudGameCartridge(IRuntime runtime) : base(runtime)
    {
    }

    public override CartridgeConfig CartridgeConfig => new(new Point(1920, 1080));

    public IEnumerable<ILoadEvent> LoadEvents(Painter painter)
    {
        yield return new AssetLoadEvent("Sheet",
            () => new GridBasedSpriteSheet(Client.Assets.GetTexture("cat/sheet"), new Point(511)));
    }

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
