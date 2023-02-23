using ExplogineCore;
using ExplogineMonoGame;
using ExplogineMonoGame.Cartridges;
using ExplogineMonoGame.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace LudJam;

public class LudCoreCartridge : MultiCartridge
{
    private bool _isHoldingEscape;
    private float _escapeTimer;

    public LudCoreCartridge(IRuntime runtime) : base(runtime,
        new DispatchCartridge(runtime),
        new LudGameCartridge(runtime),
        new LudEditorCartridge(runtime)
    )
    {
        Instance = this;
    }

    public static LudCoreCartridge Instance { get; private set; }

    public override void OnCartridgeStarted()
    {
        base.OnCartridgeStarted();
        G.Music.Initialize();
        
        if (CurrentCartridge is DispatchCartridge dispatch)
        {
            dispatch.Go(this);
        }
    }

    protected override void BeforeUpdate(float dt)
    {
        G.Music.UpdateTween(dt);

        if (_isHoldingEscape)
        {
            _escapeTimer += dt;

            if (_escapeTimer > 1)
            {
                RegenerateCartridge<LudGameCartridge>();
                SwapTo<LudGameCartridge>();
            }
        }
        else
        {
            _escapeTimer = 0;
        }
    }

    protected override void BeforeUpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
        if (_escapeTimer > 1)
        {
            input.Keyboard.Consume(Keys.Escape);
        }

        _isHoldingEscape = input.Keyboard.GetButton(Keys.Escape).IsDown;
    }

    protected override void AfterDraw(Painter painter)
    {
        if (_escapeTimer > 0)
        {
            painter.BeginSpriteBatch();
            painter.DrawStringAtPosition(Client.Assets.GetFont("cat/Font", 72), "Resetting", Vector2.Zero, new DrawSettings{Color = Color.White.WithMultipliedOpacity(_escapeTimer)});
            painter.EndSpriteBatch();
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
