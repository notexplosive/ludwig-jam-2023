namespace LudJam;

public interface ISerializedContent
{
    public string Name { get; }
    void AddToLevel(Level level);
}
