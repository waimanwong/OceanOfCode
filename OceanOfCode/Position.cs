using System;
/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/

public struct Position
{
    public int x;
    public int y;

    public static Position None = new Position(int.MaxValue, int.MaxValue);

    public Position(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public bool IsNot(Position otherPosition)
    {
        return otherPosition.x != x || otherPosition.y != y;
    }

    public int DistanceTo(Position p)
    {
        return Math.Abs(p.x - this.x) + Math.Abs(p.y - this.y);
    }

    /// <summary>
    /// Eg: for position(3,4) returns "3 4"
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"{x.ToString()} {y.ToString()}";
    }

    public Position Translate(int dx, int dy)
    {
        return new Position(x + dx, y + dy);
    }
}
