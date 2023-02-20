using System.Diagnostics.Contracts;
using Microsoft.Xna.Framework;

namespace FenestraSceneGraph.Data;

public class Direction
{
    public static readonly Direction Up = new("Up", new Point(0, -1));
    public static readonly Direction Right = new("Right", new Point(1, 0));
    public static readonly Direction Down = new("Down", new Point(0, 1));
    public static readonly Direction Left = new("Left", new Point(-1, 0));
    public static readonly Direction None = new("None", Point.Zero);

    private readonly Point _internalPoint;
    private readonly string _name;

    private Direction(string name, Point givenPoint)
    {
        _name = name;
        _internalPoint = givenPoint;
    }

    public Direction Previous
    {
        get
        {
            if (this == Direction.Up)
            {
                return Direction.Left;
            }

            if (this == Direction.Right)
            {
                return Direction.Up;
            }

            if (this == Direction.Down)
            {
                return Direction.Right;
            }

            if (this == Direction.Left)
            {
                return Direction.Down;
            }

            return Direction.None;
        }
    }

    public Direction Next
    {
        get
        {
            if (this == Direction.Up)
            {
                return Direction.Right;
            }

            if (this == Direction.Right)
            {
                return Direction.Down;
            }

            if (this == Direction.Down)
            {
                return Direction.Left;
            }

            if (this == Direction.Left)
            {
                return Direction.Up;
            }

            return Direction.None;
        }
    }

    public Direction Opposite
    {
        get
        {
            if (this == Direction.Up)
            {
                return Direction.Down;
            }

            if (this == Direction.Right)
            {
                return Direction.Left;
            }

            if (this == Direction.Down)
            {
                return Direction.Up;
            }

            if (this == Direction.Left)
            {
                return Direction.Right;
            }

            return Direction.None;
        }
    }

    public override string ToString()
    {
        return _name;
    }

    public Point ToPoint()
    {
        return _internalPoint;
    }

    public static Direction PointToDirection(Point point)
    {
        var absX = Math.Abs(point.X);
        var absY = Math.Abs(point.Y);
        if (absX > absY)
        {
            if (point.X < 0)
            {
                return Direction.Left;
            }

            if (point.X > 0)
            {
                return Direction.Right;
            }
        }

        if (absX < absY)
        {
            if (point.Y < 0)
            {
                return Direction.Up;
            }

            if (point.Y > 0)
            {
                return Direction.Down;
            }
        }

        return Direction.None;
    }

    public float Radians()
    {
        if (this == Direction.Up)
        {
            return MathF.PI;
        }

        if (this == Direction.Right)
        {
            return MathF.PI + MathF.PI / 2;
        }

        if (this == Direction.Down)
        {
            return 0;
        }

        if (this == Direction.Left)
        {
            return MathF.PI / 2;
        }

        return 0;
    }

    public override bool Equals(object? obj)
    {
        return obj is Direction direction &&
               _internalPoint.Equals(direction._internalPoint);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_internalPoint);
    }

    public static bool operator ==(Direction left, Direction right)
    {
        left ??= Direction.None;
        right ??= Direction.None;
        return left._internalPoint == right._internalPoint;
    }

    public static bool operator !=(Direction left, Direction right)
    {
        return !(left == right);
    }

    public bool IsHorizontal()
    {
        return this == Direction.Left || this == Direction.Right;
    }

    public bool IsVertical()
    {
        return this == Direction.Up || this == Direction.Down;
    }

    [Pure]
    public Vector2 ToVector2()
    {
        return new Vector2(_internalPoint.X, _internalPoint.Y);
    }
}
