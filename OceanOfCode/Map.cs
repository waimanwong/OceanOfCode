using System;
using System.Linq;
using System.Collections.Generic;

static class Map
{
    public static int Height;
    public static int Width;
    private static string[] Rows;

    private static char Water = '.';
    
    public static HashSet<Position> WaterPositions = new HashSet<Position>();

    public static void InitializeMap(int height, int width, string[] rows)
    {
        Height = height;
        Width = width;
        Rows = rows;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var position = new Position(x, y);

                if (IsWater(position))
                {
                    WaterPositions.Add(position);
                }
            }
        }
    }

    public static Position GetRandomWaterPosition()
    {
        var position = Position.None;

        while (IsWater(position) == false)
        {
            var random = new Random((int)System.Diagnostics.Stopwatch.GetTimestamp());
            var x = random.Next(0, Width - 1);
            var y = random.Next(0, Height - 1);

            position = new Position(x, y);
        }

        return position;
    }

    public static bool IsWater(Position coord)
    {
        var x = coord.x;
        var y = coord.y;

        return 
            (0 <= x && x < Width) && 
            (0 <= y && y < Height) &&
            Rows[y][x] == Water;
    }

    /// <summary>
    /// Returns water neighbors positions 
    /// </summary>
    /// <param name="fromPosition"></param>
    /// <returns></returns>
    public static List<(Position, Direction)> GetWaterNeighborPositions(Position fromPosition)
    {
        var positions = new List<(Position, Direction)>(4);

        foreach (var directionDelta in Player.FourDirectionDeltas)
        {
            var dx = directionDelta.Value.Item1;
            var dy = directionDelta.Value.Item2;
            var waterNeighbor = fromPosition.Translate(dx, dy);
            if(Map.IsWater(waterNeighbor))
            {
                positions.Add( (waterNeighbor, directionDelta.Key) );
            }
        }
        return positions;
    }

    public static HashSet<Position> GetSectorWaterPositions(int sector)
    {
        var waterSectorPositions = new HashSet<Position>();
        var (topLeft, bottomRight) = GetSectorBounds(sector);

        for(int x = topLeft.x; x <= bottomRight.x; x++)
        {
            for(int y = topLeft.y; y <= bottomRight.y; y++)
            {
                var p = new Position(x, y);
                if(WaterPositions.Contains(p))
                {
                    waterSectorPositions.Add(p);
                }
            }
        }
        return waterSectorPositions;
    }

    private static (Position, Position) GetSectorBounds(int sector)
    {
        switch (sector)
        {
            case 1:
                return (new Position(0, 0), new Position(4, 4));
            case 2:
                return (new Position(5, 0), new Position(9, 4));
            case 3:
                return (new Position(10, 0), new Position(14, 4));

            case 4:
                return (new Position(0, 5), new Position(4, 9));
            case 5:
                return (new Position(5, 5), new Position(9, 9));
            case 6:
                return (new Position(10, 5), new Position(14, 9));

            case 7:
                return (new Position(0, 10), new Position(4, 14));
            case 8:
                return (new Position(5, 10), new Position(9, 14));
            case 9:
                return (new Position(10, 10), new Position(14, 14));

            default:
                throw new NotSupportedException();
        }
    }
}
