using ExplogineCore;
using ExplogineMonoGame;
using ExplogineMonoGame.Cartridges;
using ExplogineMonoGame.Data;
using Microsoft.Xna.Framework;

namespace LudJam;

public class LudCoreCartridge : MultiCartridge
{
    public LudCoreCartridge(IRuntime runtime) : base(runtime,
        new DispatchCartridge(runtime),
        new LudGameCartridge(runtime),
        new LudEditorCartridge(runtime)
    )
    {
    }

    public override void OnCartridgeStarted()
    {
        base.OnCartridgeStarted();
        
        if (CurrentCartridge is DispatchCartridge dispatch)
        {
            dispatch.Go(this);
        }
    }

    private class DispatchCartridge : Cartridge, ICommandLineParameterProvider
    {
        public DispatchCartridge(IRuntime runtime) : base(runtime)
        {
        }

        public override CartridgeConfig CartridgeConfig => new(new Point(1920, 1080));

        public void AddCommandLineParameters(CommandLineParametersWriter parameters)
        {
            parameters.RegisterParameter<bool>("editor");
        }

        public override void OnCartridgeStarted()
        {
        }

        public override void Draw(Painter painter)
        {
        }

        public override void Update(float dt)
        {
        }

        public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
        {
        }

        public override bool ShouldLoadNextCartridge()
        {
            return false;
        }

        public override void Unload()
        {
        }

        public void Go(LudCoreCartridge coreCartridge)
        {
            if (Client.Args.GetValue<bool>("editor"))
            {
                coreCartridge.SwapTo<LudEditorCartridge>();
            }
            else
            {
                coreCartridge.SwapTo<LudGameCartridge>();
            }
        }
    }
}
