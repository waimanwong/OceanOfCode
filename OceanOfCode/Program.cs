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

public struct Position
{
    public int x;
    public int y;

    public static Position OutOfBOund = new Position(int.MinValue, int.MinValue);

    public static Position None = new Position(int.MaxValue, int.MaxValue);

    public Position(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public static bool operator ==(Position p1, Position p2)
    {
        return p1.Equals(p2);
    }

    public static bool operator !=(Position p1, Position p2)
    {
        return !p1.Equals(p2);
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
}

static class Map
{
    public static int Height;
    public static int Width;
    public static string[] Rows;

    private static char Water = '.';
    private static char Island = 'x';

    public static Dictionary<int, HashSet<Position>> PossibleMovesByCount;

    private static int[][] PossibleMoveCount;
   
    public static void InitializeMap(int height, int width, string[] rows)
    {
        Height = height;
        Width = width;
        Rows = rows;

        PossibleMoveCount = new int[height][];
        PossibleMovesByCount = new Dictionary<int, HashSet<Position>>();
        for (int i = 1; i < 5; i++)
        {
            PossibleMovesByCount[i] = new HashSet<Position>();
        }

        for (int y = 0; y < height; y++ )
        {
            PossibleMoveCount[y] = new int[width];

            for(int x = 0; x < width; x++)
            {
                var position = new Position(x, y);

                if (IsWater(position))
                {
                    var possibleMoveCount = Map.GetNeighborPositions(position)
                        .Count( neighpos => IsWater(neighpos.Item1));

                    PossibleMoveCount[y][x] = possibleMoveCount;

                    if (possibleMoveCount > 0)
                        PossibleMovesByCount[possibleMoveCount].Add(position);
                }
            }
        }
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

    public static List<(Position, Direction)> GetNeighborPositions(Position fromPosition)
    {
        var neighborPositions = new List<(Position, Direction)>(4);
        foreach(var direction in Player.AllDirections)
        {
            switch (direction)
            {
                case Direction.E:
                    if (fromPosition.x != Width - 1) 
                        neighborPositions.Add((new Position(fromPosition.x + 1, fromPosition.y), direction));
                    break;

                case Direction.N:
                    if (fromPosition.y != 0) 
                        neighborPositions.Add((new Position(fromPosition.x, fromPosition.y - 1), direction));
                    break;

                case Direction.S:
                    if (fromPosition.y != Height - 1)
                        neighborPositions.Add((new Position(fromPosition.x, fromPosition.y + 1), direction));
                    break;

                case Direction.W:
                    if (fromPosition.x != 0)
                        neighborPositions.Add((new Position(fromPosition.x - 1, fromPosition.y), direction));
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
        return neighborPositions;
    }

    public static bool IsInMap(Position p)
    {
        return 0 <= p.x && p.x < Width &&
            0 <= p.y && p.y < Height;
    }

}

class StartingPositionComputer
{
    public Position ComputeInitialPosition()
    {
        var smallestMoves = Map.PossibleMovesByCount
            .Where(kvp => kvp.Value.Count > 0)
            .OrderBy(kvp => kvp.Key)
            .First()
            .Value;

        var random = new Random();

        Player.Debug(smallestMoves.Count.ToString());
        
        return smallestMoves.ElementAt(random.Next(0, smallestMoves.Count - 1));
    }
}

class GameState
{
    public readonly Position MyPosition;
    public readonly List<Action> OpponentActions;
    public readonly int TorpedoCooldown;

    public GameState(Position myPosition, List<Action> opponentActions, int torpedoCooldown)
    {
        MyPosition = myPosition;
        OpponentActions = opponentActions;
        TorpedoCooldown = torpedoCooldown;
    }

    public bool TorpedoAvailable => TorpedoCooldown == 0;
}

public enum Direction { N, S, E, W }

public enum Power { UNKNOWN, TORPEDO }

#region actions
public abstract class Action
{
    private static string _separator = "|";

    public static List<Action> Parse(string txtOpponentOrders)
    {
        var orders = txtOpponentOrders.Split(_separator.ToCharArray());

        var actions = new List<Action>();

        foreach(var order in orders)
        {
            var tokens = order.Split(' ');
            var cmd = tokens[0];
            switch(cmd)
            {
                case "MOVE":
                    Enum.TryParse<Direction>(tokens[1], out var direction);
                    actions.Add(new Move(direction, Power.UNKNOWN));
                    break;

                case "TORPEDO":
                    var x = int.Parse(tokens[1]);
                    var y = int.Parse(tokens[2]);
                    var position = new Position(x, y);
                    actions.Add(new Torpedo(position));

                    History.LastOpponentTorpedoPosition = position;
                    
                    break;

                case "SURFACE":
                    var sector = int.Parse(tokens[1]);
                    actions.Add(new Surface(sector));
                    break;

                default:
                    //ignore 
                    break;
            }
        }

        return actions;
    }

    public static string ToText(List<Action> actions)
    {
        return string.Join(_separator, actions.Select(a => a.ToString()).ToArray());

    }
}

class Move : Action
{
    private Direction _direction;
    private Power _power;

    public Move(Direction d, Power power)
    {
        _direction = d;
        _power = power;
    }

    public override string ToString()
    {
        return $"MOVE {_direction.ToString()} {_power.ToString()}";
    }
}

class Surface : Action
{
    public readonly int sector;

    public Surface(int sector)
    {
        this.sector = sector;
    }

    public Surface() : this(-1) { }

    public override string ToString()
    {
        return "SURFACE";
    }
}

class Torpedo: Action
{
    public static int Range = 4;
    public readonly Position TargetPosition;

    public Torpedo(Position position)
    {
        TargetPosition = position;
    }

    public override string ToString()
    {
        return $"TORPEDO {TargetPosition.ToString()}";
    }
}
#endregion

class AI
{   
    private readonly GameState _gameState;
 
    public AI(GameState gameState)
    {
        _gameState = gameState;
    }

    public List<Action> ComputeActions()
    {
        var actions = new List<Action>();

        var selectedMove = SelectMove();
        actions.Add(selectedMove);

        return actions;
    }

    private Action SelectMove()
    {
        var possibleMoves = GetPossibleDirectionsForMove();
        var possibleMoveCount = possibleMoves.Count;

        if (possibleMoveCount == 0)
        {
            return new Surface();
        }

        if (possibleMoveCount == 1)
        {
            return new Move(possibleMoves.Single().Item2, Power.TORPEDO);
        }

        return EvaluateBestMove(possibleMoves);
    }

    private static Action EvaluateBestMove(List<(Position, Direction)> possibleMoves)
    {
        var bestDirection = Direction.N;
        var bestScore = 0;

        foreach (var possibleMove in possibleMoves)
        {
            var floodFill = new FloodFill();
            var filledRegion = floodFill.Run(possibleMove.Item1);

            var score = filledRegion.Count;

            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = possibleMove.Item2;
            }
        }

        return new Move(bestDirection, Power.TORPEDO);
    }

    private List<(Position, Direction)> GetPossibleDirectionsForMove()
    {
        var possibleDirections = new List<Direction>();
        var myPosition = _gameState.MyPosition;

        var waterNeighborPositions = Map.GetNeighborPositions(myPosition)
            .Where(x => Map.IsWater(x.Item1))
            .Where(x => History.VisitedPositions.Contains(x.Item1) == false);

        return waterNeighborPositions.ToList();
    }
}

public class FloodFill
{
    public HashSet<Position> _alreadyVisitedPositions = History.VisitedPositions;

    public HashSet<Position> _remainingPositionsToVisit = new HashSet<Position>();

    /// <summary>
    /// Return the filled positions
    /// </summary>
    /// <param name="startPosition"></param>
    /// <returns></returns>
    public HashSet<Position> Run(Position startPosition)
    {
        if (_alreadyVisitedPositions.Contains(startPosition))
            return _remainingPositionsToVisit;

        // Set the color of node to replacement-color.
        _remainingPositionsToVisit.Add(startPosition);

        var q = new Queue<Position>();
        q.Enqueue(startPosition);

        while(q.Count > 0)
        {
            var currentPosition = q.Dequeue();

            var neighbors = Map.GetNeighborPositions(currentPosition)
                .Where(x => Map.IsWater(x.Item1))
                .ToList();

            foreach(var neighbor in neighbors)
            {
                if(_alreadyVisitedPositions.Contains(neighbor.Item1) == false)
                {
                    if(_remainingPositionsToVisit.Contains(neighbor.Item1) == false)
                    {
                        _remainingPositionsToVisit.Add(neighbor.Item1);
                        q.Enqueue(neighbor.Item1);
                    }
                }

            }
        }

        return _remainingPositionsToVisit;
    }
}


public static class History
{
    public static readonly HashSet<Position> VisitedPositions = new HashSet<Position>();

    public static Position LastOpponentTorpedoPosition = Position.None;

    public static void Visit(Position position)
    {
        VisitedPositions.Add(position);

        if(LastOpponentTorpedoPosition == position)
        {
            LastOpponentTorpedoPosition = Position.None;
        }
    }
}

class Player
{
    public static Direction[] AllDirections = new[]
    {
        Direction.E,
        Direction.N,
        Direction.S, Direction.W
    };

    public static void Debug(string message)
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

            Debug(line);
            Debug($"Sonar Rsult: {Console.ReadLine()}");
            var txtOpponentOrders = Console.ReadLine();

            var opponentOrders = Action.Parse(txtOpponentOrders);

            inputs = line.Split(' ');
            int x = int.Parse(inputs[0]);
            int y = int.Parse(inputs[1]);

            var myPosition = new Position(x, y);

            History.Visit(myPosition);
            
            int myLife = int.Parse(inputs[2]);
            int oppLife = int.Parse(inputs[3]);
            int torpedoCooldown = int.Parse(inputs[4]);
            int sonarCooldown = int.Parse(inputs[5]);
            int silenceCooldown = int.Parse(inputs[6]);
            int mineCooldown = int.Parse(inputs[7]);

            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");

            var gameState = new GameState(myPosition, opponentOrders, torpedoCooldown);
            var ai = new AI(gameState);

            var actions = ai.ComputeActions();
            
            if(actions.OfType<Surface>().Any())
            {
                History.VisitedPositions.Clear();
            }

            Console.WriteLine(Action.ToText( actions));
        }
    }
}