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

    public static Position GetNeighborPosition(Position position, Direction direction)
    {
        switch(direction)
        {
            case Direction.E:
                return (position.x == Width - 1) ? Position.OutOfBOund : new Position(position.x + 1, position.y);
            case Direction.N:
                return (position.y == 0) ? Position.OutOfBOund : new Position(position.x, position.y - 1);
            case Direction.S:
                return (position.y == Height - 1) ? Position.OutOfBOund : new Position(position.x, position.y + 1);
            case Direction.W:
                return (position.x == 0) ? Position.OutOfBOund : new Position(position.x - 1, position.y);

            default:
                throw new NotImplementedException();
        }
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

class GameState
{
    public readonly Position MyPosition;
    public readonly List<Action> OpponentActions;

    public GameState(Position myPosition, List<Action> opponentActions)
    {
        MyPosition = myPosition;
        OpponentActions = opponentActions;
    }
        
}

public enum Direction { N, S, E, W }

public enum Power { UNKNOWN, TORPEDO }

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

class Move: Action
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

class AI
{
    private static Direction[] AllDirections = new Direction[4] { Direction.E, Direction.N, Direction.S, Direction.W };

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

        if (History.LastOpponentTorpedoPosition != Position.None)
        {   
            Player.Debug($"Move as close as possible to the last opponent torpedo at ({History.LastOpponentTorpedoPosition.ToString()})");
            return MoveTowardPosition(possibleMoves, History.LastOpponentTorpedoPosition);
        }

        return RandomMove(possibleMoves, possibleMoveCount);

    }

    private static Action RandomMove(List<(Direction, Position)> possibleMoves, int possibleMoveCount)
    {
        Player.Debug("Random move");

        var random = new Random();
        var randomDirection = possibleMoves[random.Next(0, possibleMoveCount - 1)].Item1;

        return new Move(randomDirection, Power.TORPEDO);
    }

    private static Action MoveTowardPosition(List<(Direction, Position)> possibleMoves, Position targetPosition)
    {
        var move = possibleMoves
                    .OrderBy(x => x.Item2.DistanceTo(targetPosition))
                    .First();

        return new Move(move.Item1, Power.TORPEDO);
    }

    private List<(Direction, Position)> GetPossibleDirectionsForMove()
    {
        var possibleDirections = new List<Direction>();
        var myPosition = _gameState.MyPosition;

        var waterNeighborPositions = AllDirections
            .Select(direction =>  (direction, Map.GetNeighborPosition(myPosition, direction)))
            .Where(x => x.Item2 != Position.OutOfBOund && Map.IsWater(x.Item2))
            .Where(x => History.VisitedPositions.Contains(x.Item2) == false);

        return waterNeighborPositions.ToList();
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

            var gameState = new GameState(myPosition, opponentOrders);
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