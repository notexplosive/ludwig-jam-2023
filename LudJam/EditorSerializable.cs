using System;
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

    private Func<Actor,ISerializedContent>? SerializeFunction { get; set; }

    public EditorSerializable Init(Func<Actor,ISerializedContent> serializeFunction)
    {
        SerializeFunction = serializeFunction;
        return this;
    }

    public override void Draw(Painter painter)
    {
    }

    public override void UpdateInput(ConsumableInput input, HitTestStack hitTestStack)
    {
    }

    public ISerializedContent Serialize()
    {
        if (SerializeFunction == null)
        {
            throw new Exception("No serializer function defined");
        }
        return SerializeFunction(Actor);
    }
}