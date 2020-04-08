using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/


public enum Direction { N, S, E, W }

public enum Power { UNKNOWN, TORPEDO, SILENCE, SONAR, MINE }

class Player
{
    public static Direction[] AllDirections = new[] { Direction.E, Direction.N, Direction.S, Direction.W };
    public static Dictionary<Direction, (int, int)> FourDirectionDeltas = new Dictionary<Direction, (int, int)>
    {
        {  Direction.S, (0, 1) },
        {  Direction.W, (-1, 0) },
        {  Direction.E, (1, 0) },
        {  Direction.N, (0, -1) },
    };

    public static (int, int)[] EightDirectionDeltas = new (int, int)[]
    {
        (-1,-1), (0, -1), (1, -1),
        (-1, 0),          (1,  0),
        (-1, 1), (0,  1), (1,  1)
    };

    public static void Debug(string message)
    {
        //Console.Error.WriteLine(message);
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
            rows.Add(Console.ReadLine());
        }

        Map.InitializeMap(height, width, rows.ToArray());

        // Write an action using Console.WriteLine()
        // To debug: Console.Error.WriteLine("Debug messages...");

        var initialPosition = new StartingPositionComputer()
            .EvaluateBestPosition();
        Console.WriteLine(initialPosition.ToString());

        MySubmarine.MoveMySubmarine((initialPosition, Direction.E), Power.MINE);

        var mylastActions = new List<Action>();

        // game loop
        while (true)
        {
            //MySubmarine.Debug();

            var line = Console.ReadLine();
            var sonarLine = Console.ReadLine();
            var txtOpponentOrders = Console.ReadLine();

            //Debug(line);
            //Debug($"Sonar Rsult: {sonarLine}");
            //Debug($"txtOpponentOrders: {txtOpponentOrders}");

            inputs = line.Split(' ');
            int x = int.Parse(inputs[0]);
            int y = int.Parse(inputs[1]);

            var myPosition = new Position(x, y);
            
            int myLife = int.Parse(inputs[2]);
            int oppLife = int.Parse(inputs[3]);
            int torpedoCooldown = int.Parse(inputs[4]);
            int sonarCooldown = int.Parse(inputs[5]);
            int silenceCooldown = int.Parse(inputs[6]);
            int mineCooldown = int.Parse(inputs[7]);

            OpponentSubmarine.UpdateState(oppLife, txtOpponentOrders, mylastActions);
            OpponentSubmarine.Debug();

            MySubmarine.JustTriggeredWeapons.Clear();

            var gameState = new GameState(torpedoCooldown, sonarCooldown, silenceCooldown, mineCooldown);

            var ai = new AI(gameState);

            var actions = ai.ComputeActions();

            MySubmarine.ApplyActions(actions);            
            //MySubmarine.Debug();

            mylastActions = actions;

            Console.WriteLine(Action.ToText(actions));
        }
    }

}