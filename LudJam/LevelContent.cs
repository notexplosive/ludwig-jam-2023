using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace LudJam;

public class LevelContent
{
    public List<Rectangle> Walls = new();
    public List<EntityData> Entities = new();
    public int Par = 1;
}

public readonly record struct EntityData(string Name, Vector2 Position);
