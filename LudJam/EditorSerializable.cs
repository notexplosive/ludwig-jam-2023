using ExplogineMonoGame;
using ExplogineMonoGame.Data;
using Fenestra.Components;
using FenestraSceneGraph;

namespace LudJam;

public class EditorSerializable : BaseComponent
{
    public EditorSerializable(Actor actor) : base(actor)
    {
    }

    public string? SerialString { get; private set; }

    public EditorSerializable Init(string serialString)
    {
        SerialString = serialString;
        return this;
    }

    public override void Draw(Painter painter)
    {
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
    }
}
