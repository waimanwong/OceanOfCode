using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/


struct Position
{
    public int x;
    public int y;

    public Position(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public override string ToString()
    {
        return $"{x.ToString()} {y.ToString()}";
    }
}

static class Map
{
    public static int Height;
    public static int Width;
    public static string[] Rows;

    private static char Water = '.';
    private static char Island = 'x';
   
    public static void InitializeMap(int height, int width, string[] rows)
    {
        Height = height;
        Width = width;
        Rows = rows;
    }

    public static bool IsWater(Position coord)
    {
        var (x, y) = (coord.x, coord.y);

        return Rows[y][x] == Water;
    }
        
    public static bool IsIsland(Position coord)
    {
        var (x, y) = (coord.x, coord.y);

        return Rows[y][x] == Island;
    }

    public static List<Position> GetWaterPositions()
    {
        var waterPositions = new List<Position>();
        for(int y= 0; y< Height; y++)
        {
            for(int x = 0; x < Width; x++)
            {
                var position = new Position(x, y);
                if (IsWater(position))
                {
                    waterPositions.Add(position);
                }
            }
        }

        return waterPositions;
    }
}

class StartingPositionComputer
{
    public Position ComputeInitialPosition()
    {
        var waterPositions = Map.GetWaterPositions().ToArray();

        var random = new Random();

        return waterPositions[random.Next(0, waterPositions.Length - 1)];
    }
}

class Player
{
    static void Debug(string message)
    {
        Console.Error.WriteLine(message);
    }

    static void Main(string[] args)
    {
        string[] inputs;
        inputs = Console.ReadLine().Split(' ');
        int width = int.Parse(inputs[0]);
        int height = int.Parse(inputs[1]);
        int myId = int.Parse(inputs[2]);
        List<string> rows = new List<string>(height);
        for (int i = 0; i < height; i++)
        {
            rows.Add( Console.ReadLine());
        }

        Map.InitializeMap(height, width, rows.ToArray());

        // Write an action using Console.WriteLine()
        // To debug: Console.Error.WriteLine("Debug messages...");

        var initialPosition = new StartingPositionComputer().ComputeInitialPosition();
        Console.WriteLine(initialPosition.ToString());

        // game loop
        while (true)
        {
            string line = Console.ReadLine();

            Console.Error.WriteLine(line);
            Console.Error.WriteLine($"Sonar Rsult: {Console.ReadLine()}");
            Console.Error.WriteLine($"OpponentOrders: {Console.ReadLine()}");

            inputs = line.Split(' ');
            int x = int.Parse(inputs[0]);
            int y = int.Parse(inputs[1]);
            int myLife = int.Parse(inputs[2]);
            int oppLife = int.Parse(inputs[3]);
            int torpedoCooldown = int.Parse(inputs[4]);
            int sonarCooldown = int.Parse(inputs[5]);
            int silenceCooldown = int.Parse(inputs[6]);
            int mineCooldown = int.Parse(inputs[7]);

            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");

            Console.WriteLine("MOVE N TORPEDO");
        }
    }
}