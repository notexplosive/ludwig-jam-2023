using ExplogineMonoGame;
using ExplogineMonoGame.Data;
using Microsoft.Xna.Framework;

namespace LudJam;

public class EditorState
{
    public readonly Camera Camera = new(new RectangleF(0, 0, 1920, 1080), new Point(1920, 1080));
    public readonly Level Level = new();
    public string? SavedName = null;
    public Mode CurrentMode = Mode.Main;
}
