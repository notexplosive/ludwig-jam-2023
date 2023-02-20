#nullable enable
namespace Fenestra;

public interface ISceneMessage
{
}

public readonly record struct HitStunFreezeMessage(float Duration) : ISceneMessage;

public readonly record struct StringSceneMessage(string Content) : ISceneMessage;

public readonly record struct WindowStoppedDraggingMessage : ISceneMessage;

public readonly record struct WindowStartedDraggingMessage : ISceneMessage;

