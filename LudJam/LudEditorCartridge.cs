using ExplogineMonoGame;
using ExplogineMonoGame.Cartridges;
using ExplogineMonoGame.Data;
using Microsoft.Xna.Framework;

namespace LudJam;

public class LudEditorCartridge : NoProviderCartridge
{
    public LudEditorCartridge(IRuntime runtime) : base(runtime)
    {
    }

    public override CartridgeConfig CartridgeConfig => new(new Point(1920, 1080));
    
    public override void OnCartridgeStarted()
    {
    }

    public override void Draw(Painter painter)
    {
        painter.BeginSpriteBatch();
        painter.DrawDebugStringAtPosition("Editor", new Vector2(0), new DrawSettings());
        painter.EndSpriteBatch();
    }

    public override void Update(float dt)
    {
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
    }
}
