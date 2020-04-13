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
    
    public static Dictionary<Direction,Direction> OppositeDirection = new Dictionary<Direction, Direction>()
    {
        { Direction.E, Direction.W },
        { Direction.W, Direction.E },
        { Direction.N, Direction.S },
        { Direction.S, Direction.N },
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

        var initialPosition = new StartingPositionComputer().EvaluateBestPosition();
        Console.WriteLine(initialPosition.ToString());

        MySubmarine.MoveMySubmarine((initialPosition, Direction.E), Power.MINE);

        var mylastActions = new List<Action>();

        var stopwatch = Stopwatch.StartNew();
        
        // game loop
        while (true)
        {
            var line = Console.ReadLine();
            var sonarLine = Console.ReadLine();
            var txtOpponentOrders = Console.ReadLine();

            var start = stopwatch.ElapsedMilliseconds;

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

            var opponentActions = Action.Parse(txtOpponentOrders);
            

            Player.Debug("*** OpponentSubmarine.UpdateState ***");
            OpponentSubmarine.UpdateState(oppLife, opponentActions, mylastActions);
            OpponentSubmarine.Debug();
            Player.Debug("");

            Player.Debug("*** MySubmarine.UpdateState ***");
            MySubmarine.UpdateState(myLife, mylastActions, opponentActions);
            MySubmarine.Debug();
            
            var stealthScore = $"{MySubmarine.PossiblePositions.Count} - {OpponentSubmarine.PossiblePositions.Count}";
        
            var gameState = new GameState(torpedoCooldown, sonarCooldown, silenceCooldown, mineCooldown);
            var ai = new AI(gameState);
            var actions = ai.ComputeActions();

            MySubmarine.ApplyActions(actions);            
            
            mylastActions = actions;

            var messageAction = new MessageAction($"{stealthScore} ({stopwatch.ElapsedMilliseconds - start}ms)");
            actions.Add(messageAction);

            Console.WriteLine(Action.ToText(actions));
        }
    }

}