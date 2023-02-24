using ExplogineMonoGame;
using ExplogineMonoGame.Data;
using Fenestra;
using Fenestra.Components;
using FenestraSceneGraph;
using FenestraSceneGraph.Components;

namespace LudJam;

public class ToggleWall : BaseComponent
{
    private bool _isOn;
    private readonly BoundingRectangle _boundingRectangle;
    private Actor? _solidActor;
    private bool _startOn;

    public ToggleWall(Actor actor) : base(actor)
    {
        _boundingRectangle = RequireComponent<BoundingRectangle>();
        Actor.Destroyed += () =>
        {
            SetOn(false);
        };
    }

    public override void Draw(Painter painter)
    {
        if (!_isOn)
        {
            var color = _startOn ? G.ToggleWallOnColor1 : G.ToggleWallOffColor1;
            painter.DrawLineRectangle(_boundingRectangle,new LineDrawSettings{ Color = color, Thickness = 10, Depth = Actor.Depth});
            // painter.DrawRectangle(_boundingRectangle,new DrawSettings{ Color = color.WithMultipliedOpacity(0.15f), Depth = Actor.Depth});
        }
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
        // we need this for editor
        if (_solidActor != null)
        {
            _solidActor.Position = Actor.Position;
            _solidActor.GetComponent<BoundingRectangle>()!.Init(_boundingRectangle.Rectangle.Size);
        }
    }

    public override void ReceiveBroadcast(ISceneMessage message)
    {
        if (message is JumpMessage)
        {
            Toggle();
        }
    }

    private void Toggle()
    {
        SetOn(!_isOn);
    }

    private void SetOn(bool isNowOn)
    {
        if (isNowOn)
        {
            Actor.Scene.AddDeferredAction(() =>
            {
                _solidActor = Actor.Scene.AddNewActor();
                _solidActor.Position = Actor.Position;
                _solidActor.AddComponent<BoundingRectangle>().Init(_boundingRectangle.Rectangle.Size);
                _solidActor.AddComponent<Solid>();

                if (_startOn)
                {
                    _solidActor.AddComponent<WallRenderer>().Init(G.ToggleWallOnColor1, G.ToggleWallOnColor2);
                }
                else
                {
                    _solidActor.AddComponent<WallRenderer>().Init(G.ToggleWallOffColor1, G.ToggleWallOffColor2);
                }
            });
        }
        else
        {
            _solidActor?.DestroyDeferred();
        }

        _isOn = isNowOn;
    }

    public void Init(bool startOn)
    {
        _startOn = startOn;
        SetOn(startOn);
    }
}
