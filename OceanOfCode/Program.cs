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

    public Position Translate(int dx, int dy)
    {
        return new Position(x + dx, y + dy);
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

    public static Position GetRandomWaterPosition()
    {
        var position = Position.None;

        while(IsWater(position) == false)
        {
            var random = new Random((int)System.Diagnostics.Stopwatch.GetTimestamp());
            var x = random.Next(0, Width - 1);
            var y = random.Next(0, Height - 1);

            position = new Position(x,y);
        }

        return position;
    }

    public static bool IsWater(Position coord)
    {
        var (x, y) = (coord.x, coord.y);

        return IsWater(x, y);
    }

    public static bool IsWater(int x,int y)
    {
        return (0 <= x && x < Width) &&
            (0 <= y && y < Height) &&
            Rows[y][x] == Water;
    }

        
    public static bool IsIsland(Position coord)
    {
        var (x, y) = (coord.x, coord.y);

        return Rows[y][x] == Island;
    }

    /// <summary>
    /// Returns neighbors positions whether map or land
    /// </summary>
    /// <param name="fromPosition"></param>
    /// <returns></returns>
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

    private static Position GetClosestWaterPosition(Position p, Func<Position, Position> nextPosition)
    {
        var currentPosition = p;
        while(Map.IsWater(currentPosition) == false)
        {
            currentPosition = nextPosition(currentPosition);
        }
        return currentPosition;
    }

    public static (Position, Position) GetSectorBounds(int sector)
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

class StartingPositionComputer
{
    public Position EvaluateBestPosition()
    {
        var random = new Random();

        var randomPositions = Enumerable.Range(1, 10)
            .Select(i => Map.GetRandomWaterPosition());

        var bestPosition = Position.None;
        var bestFilledRegion = new HashSet<Position>();

        foreach(var position in randomPositions)
        {
            if(bestFilledRegion.Contains(position) == true)
            {
                //Do nothings
            }
            else
            {
                var noVisitedPositions = new HashSet<Position>();
                var fillEngine = new FloodFillEngine(noVisitedPositions);
                var filledRegion = fillEngine.Run(position);

                if(filledRegion.Count > bestFilledRegion.Count)
                {
                    bestPosition = position;
                    bestFilledRegion = filledRegion;
                }
            }
        }

        return bestPosition;
    }
}

class GameState
{
    public Position MyPosition;
    public readonly List<Action> OpponentActions;

    private readonly int _torpedoCooldown;
    private readonly int _sonarCooldown;
    private readonly int _silenceCooldown;
    private readonly int _mineCooldown;

    public GameState(Position myPosition, List<Action> opponentActions, 
        int torpedoCooldown, int sonarCooldown, int silenceCooldown, int mineCooldown)
    {
        MyPosition = myPosition;
        OpponentActions = opponentActions;

        _torpedoCooldown = torpedoCooldown;
        _sonarCooldown = sonarCooldown;
        _silenceCooldown = silenceCooldown;
        _mineCooldown = mineCooldown;
    }

    public bool TorpedoAvailable => _torpedoCooldown == 0;
    public bool SonarAvailable => _sonarCooldown == 0;
    public bool SilenceAvailable => _silenceCooldown == 0;
    public bool MineAvailable => _mineCooldown == 0;

    public void MoveMySubmarine(Position newPosition)
    {
        MyPosition = newPosition;
    }

}

public enum Direction { N, S, E, W }

public enum Power { UNKNOWN, TORPEDO, SILENCE, SONAR, MINE}

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
                    actions.Add(new MoveAction(direction, Power.UNKNOWN));
                    break;

                case "TORPEDO":
                    var x = int.Parse(tokens[1]);
                    var y = int.Parse(tokens[2]);
                    var position = new Position(x, y);
                    actions.Add(new TorpedoAction(position));

                    break;

                case "SURFACE":
                    actions.Add(new SurfaceAction(int.Parse(tokens[1])));
                    break;

                case "SONAR":
                    actions.Add(new SonarAction(int.Parse(tokens[1])));
                    break;

                case "SILENCE":
                    actions.Add(new SilenceAction());
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

class MoveAction : Action
{
    public readonly Direction Direction;
    private Power _power;

    public MoveAction(Direction d, Power power)
    {
        Direction = d;
        _power = power;
    }

    public override string ToString()
    {
        return $"MOVE {Direction.ToString()} {_power.ToString()}";
    }
}

class SurfaceAction : Action
{
    public readonly int sector;

    public SurfaceAction(int sector)
    {
        this.sector = sector;
    }

    public SurfaceAction() : this(-1) { }

    public override string ToString()
    {
        return "SURFACE";
    }
}

class TorpedoAction: Action
{
    public static int Range = 4;
    public readonly Position TargetPosition;

    public TorpedoAction(Position position)
    {
        TargetPosition = position;
    }

    public override string ToString()
    {
        return $"TORPEDO {TargetPosition.ToString()}";
    }
}

class SonarAction : Action
{
    private readonly int _sector;

    public SonarAction(int sector)
    {
        _sector = sector;
    }

    public override string ToString()
    {
        return $"SONAR {_sector.ToString()}";
    }
}

class SilenceAction :Action
{
    private readonly Direction? _direction;
    private int _moves;

    public SilenceAction()
    {
        _direction = null;
    }

    public SilenceAction(Direction direction, int moves)
    {
        _direction = direction;
        _moves = moves;
    }

    public override string ToString()
    {
        return $"SILENCE {_direction.ToString()} {_moves.ToString()}";
    }
}

class MineAction : Action
{
    private readonly Direction _direction;

    public MineAction(Direction direction)
    {
        _direction = direction;
    }

    public override string ToString()
    {
        return $"MINE {_direction.ToString()}";
    }
}

class TriggerAction: Action
{
    private Position _position;

    public TriggerAction(Position position)
    {
        _position = position;
    }

    public override string ToString()
    {
        return $"TRIGGER {_position.ToString()}";
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

        var selectedMoveAction = SelectMoveAction(MySubmarine.VisitedPositions);
        actions.Add(selectedMoveAction);

        var selectedActions = SelectPowerActions(MySubmarine.VisitedPositions);
        actions.AddRange(selectedActions);

        return actions;
    }

    private List<Action> SelectPowerActions(HashSet<Position> visitedPositions)
    {
        var myPosition = _gameState.MyPosition;
        var powerActions = new List<Action>();

        //Place mine ?
        if (_gameState.MineAvailable)
        {
            if (TrySelectMinePosition(out var position, out var direction))
            {
                powerActions.Add(PlaceMine((position, direction)));
            }
        }

        //Trigger mine ?
        var placedMines = MySubmarine.GetPlacedMines();

        if (placedMines.Count > 0)
        {
            var bestMineToTrigger = Position.None;
            var enemyPossiblePositions = OpponentSubmarine.PossiblePositionCount;
            var bestScore = int.MaxValue;

            foreach(var minePosition in placedMines)
            {
                var blastedPositions = Player.EightDirectionDeltas
                    .Select(delta => minePosition.Translate(delta.Item1, delta.Item2))
                    .ToHashSet();
                blastedPositions.Add(minePosition);

                #region ignore if in the blast
                if (blastedPositions.Contains(myPosition))
                {
                    continue;
                }
                #endregion

                #region Evaluate score = remaining possible positions after blast
                var remainingPositions = OpponentSubmarine.PossiblePositions;
                foreach(var blastedPosition in blastedPositions)
                {
                    remainingPositions.Add(blastedPosition);
                }
                var remainingPositionCount = remainingPositions.Count;
                #endregion
                
                if (remainingPositionCount < bestScore)
                {
                    bestScore = remainingPositionCount;
                    bestMineToTrigger = minePosition;
                }
            }

            if(bestMineToTrigger != Position.None)
            {
                powerActions.Add(TriggerMine(bestMineToTrigger));
            }
            
        }

        return powerActions;
    }

    private  bool TrySelectMinePosition(out Position position, out Direction direction )
    {
        var myPosition = _gameState.MyPosition;
        var neighborWaterPositions = Map.GetNeighborPositions(myPosition)
            .Where(x => Map.IsWater(x.Item1))
            .ToList();

        int maxCoverage = -1;
        position = Position.None;
        direction = Direction.E;

        foreach (var item in neighborWaterPositions)
        {
            var possibleMinePosition = item.Item1;
            var possibleMineDirection = item.Item2;

            if(MySubmarine.HasPlacedMineAt(possibleMinePosition) == false)
            {
                //Maximize area of blast
                var newMines = MySubmarine.GetPlacedMines();
                newMines.Add(possibleMinePosition);

                var coverage = ComputeCoveredAreaByMines(newMines);

                Player.Debug($"Mine at {possibleMineDirection.ToString()}, coverage = {coverage.ToString()}");

                var coverageIsBetter = coverage > maxCoverage;
                
                if(coverageIsBetter)
                {
                    maxCoverage = coverage;
                    position = possibleMinePosition;
                    direction = possibleMineDirection;
                }
            }
        }

        return maxCoverage > 0 ;
    }

    private static int ComputeCoveredAreaByMines(HashSet<Position> minePositions)
    {
        var blastedPositions = new HashSet<Position>();

        foreach(var mine in minePositions)
        {
            foreach(var delta in Player.EightDirectionDeltas)
            {
                var blastedPosition = new Position(mine.x + delta.Item1, mine.y + delta.Item2);
                if(Map.IsInMap(blastedPosition) && Map.IsWater(blastedPosition))
                {
                    blastedPositions.Add(blastedPosition);
                }
            }
        }

        return blastedPositions.Count;
    }

    private Action SelectMoveAction(HashSet<Position> visitedPositions)
    {
        var fromPosition = _gameState.MyPosition;
        var possibleMoves = GetPossibleDirectionsForMove(visitedPositions, fromPosition);
        var possibleMoveCount = possibleMoves.Count;

        if (possibleMoveCount == 0)
        {
            return SurfaceMySubmarine();
        }

        if (possibleMoveCount == 1)
        {
            var possibleMove = possibleMoves.Single();
            return MoveMySubmarine(possibleMove, SelectPowerToCharge());
        }

        var bestMove = possibleMoves.First();
        var bestScore = 0;
        var bestFilledRegion = new HashSet<Position>();

        foreach (var possibleMove in possibleMoves)
        {
            if (bestFilledRegion.Contains(possibleMove.Item1))
            {
                //the possible move result in the same bestfilled region
                var freedomScore = new Func<Position, int>(pos =>
                       Map.GetNeighborPositions(pos)
                           .Count(p => Map.IsWater(p.Item1) && visitedPositions.Contains(p.Item1) == false));

                var bestPositionFreedomScore = freedomScore(bestMove.Item1);
                var currentMoveFreedomScore = freedomScore(possibleMove.Item1);

                if (currentMoveFreedomScore < bestPositionFreedomScore)
                {
                    //go toward position with least freedom
                    bestMove = possibleMove;
                }
            }
            else
            {
                var floodFillEngine = new FloodFillEngine(visitedPositions);

                var filledRegion = floodFillEngine.Run(possibleMove.Item1);

                var score = filledRegion.Count;

                if (score > bestScore)
                {
                    //Go to the position with largest region
                    bestScore = score;
                    bestMove = possibleMove;
                    bestFilledRegion = filledRegion;
                }
            }
        }

        return MoveMySubmarine(bestMove, SelectPowerToCharge());
    }

    private Power SelectPowerToCharge()
    {       
        if (_gameState.MineAvailable == false)
        {
            return Power.MINE;
        }

        if (_gameState.SilenceAvailable == false)
        {
            return Power.SILENCE;
        }

        if (_gameState.TorpedoAvailable == false)
        {
            return Power.TORPEDO;
        }

        if (_gameState.SonarAvailable == false)
        {
            return Power.SONAR;
        }
        
        return Power.MINE;
    }

    private List<(Position, Direction)> GetPossibleDirectionsForMove(HashSet<Position> visitedPositions, Position myPosition)
    {
        var possibleDirections = new List<Direction>();
        var waterNeighborPositions = Map.GetNeighborPositions(myPosition)
            .Where(x => Map.IsWater(x.Item1))
            .Where(x => visitedPositions.Contains(x.Item1) == false);

        return waterNeighborPositions.ToList();
    }

    private TorpedoAction LaunchTorpedo(Position position)
    {
        return new TorpedoAction(position);
    }

    private MineAction PlaceMine((Position, Direction ) place)
    {
        MySubmarine.PlaceMine(place.Item1);
        return new MineAction(place.Item2);
    }

    private MoveAction MoveMySubmarine((Position, Direction) move, Power power)
    {
        var newPosition = move.Item1;

        _gameState.MoveMySubmarine(newPosition);

        MySubmarine.Visit(newPosition);

        return new MoveAction(move.Item2, power);
    }
    
    private SurfaceAction SurfaceMySubmarine()
    {
        MySubmarine.ResetVisitedPositions();

        return new SurfaceAction();
    }

    private TriggerAction TriggerMine(Position p)
    {
        return new TriggerAction(p);
    }

    //private SilenceAction Silence(Direction direction, int moves)
    //{
    //    return new SilenceAction(direction, 1);
    //}
}

public class FloodFillEngine
{
    private readonly HashSet<Position> _alreadyVisitedPositions;
    private readonly HashSet<Position> _remainingPositionsToVisit;

    public FloodFillEngine(HashSet<Position> visitedPosition)
    {
        _alreadyVisitedPositions = visitedPosition;
        _remainingPositionsToVisit = new HashSet<Position>();
    }

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

public static class OpponentSubmarine
{
    private static int _health = 6;

    private static HashSet<Position> _possiblePositions = new HashSet<Position>();

    public static HashSet<Position> PossiblePositions => _possiblePositions.ToHashSet();

    public static int PossiblePositionCount => _possiblePositions.Count;

    public static void ResetPossiblePositions()
    {
        _possiblePositions = Map.PossibleMovesByCount.Values
            .SelectMany(x => x)
            .ToHashSet();
    }

    public static void EvaluateNewPossiblePositions(Action opponentAction)
    {
        if(opponentAction is MoveAction)
        {
            MoveAction moveAction = (MoveAction)opponentAction;
            var (dx, dy) = Player.FourDirectionDeltas[moveAction.Direction];

            //Add possible positions
            var currentPossiblePositions = _possiblePositions.ToHashSet();
            foreach (var currentPosition in currentPossiblePositions)
            {
                var newPosition = currentPosition.Translate(dx, dy);
                if(Map.IsInMap(newPosition) && Map.IsWater(newPosition))
                    _possiblePositions.Add(newPosition);
            }

            //Remove impossible positions
            _possiblePositions = _possiblePositions
                .Where(p => {
                    var previousPosition = p.Translate(-dx,-dy);
                    return Map.IsWater(previousPosition) && _possiblePositions.Contains(previousPosition); 
                })
                .ToHashSet();
        }
        else if (opponentAction is SurfaceAction)
        {
            ResetPossiblePositions();

            SurfaceAction surfaceAction = (SurfaceAction)opponentAction;
            var (topLeftPosition, bottomRigthPosition) = Map.GetSectorBounds(surfaceAction.sector);
            _possiblePositions = _possiblePositions
                .Where(p => (topLeftPosition.x <= p.x && p.x <= bottomRigthPosition.x) &&
                           (topLeftPosition.y <= p.y && p.y <= bottomRigthPosition.y))
                .ToHashSet();
        }
        else if(opponentAction is TorpedoAction)
        {
            TorpedoAction torpedoAction = (TorpedoAction)opponentAction;
            _possiblePositions = _possiblePositions
                .Where(p => p.DistanceTo(torpedoAction.TargetPosition) <= 4)
                .ToHashSet();
        }
        else if(opponentAction is SilenceAction)
        {
            ResetPossiblePositions();
        }
        else
        {
            Player.Debug($"ignore opponent: {opponentAction.GetType().ToString()}");
        }
    }

    public static void Debug()
    {
        StringBuilder row = new StringBuilder();

        Player.Debug("possible opponent map:");
        for (int y = 0; y < Map.Height; y++)
        {
            row.Clear();
            row.Append('|');
            for (int x = 0; x < Map.Width; x++)
            {
                if(_possiblePositions.Contains(new Position(x,y)))
                {
                    row.Append(' ');
                }
                else
                {
                    row.Append('X');
                }
            }
            row.Append('|');
            Player.Debug(row.ToString());
        }
        Player.Debug("---------------------------");
    }

    public static bool TryGuessEnemyPosition(out Position guessedEnenmyPosition)
    {
        guessedEnenmyPosition = Position.None;

        foreach(var position in _possiblePositions)
        {
            var neighbors = Player.FourDirectionDeltas.Values
                .Select(delta => position.Translate(delta.Item1, delta.Item2))
                .Where(p => Map.IsInMap(p) && Map.IsWater(p));

            var possibleNeighborCount = neighbors.Count(p => _possiblePositions.Contains(p));

            if(possibleNeighborCount <= 2)
            {
                guessedEnenmyPosition = position;
            }
        }

        return guessedEnenmyPosition != Position.None;
    }
}

public static class MySubmarine
{
    private static readonly HashSet<Position> _visitedPositions = new HashSet<Position>();

    public static readonly HashSet<Position> MinePositions = new HashSet<Position>();

    public static void Visit(Position position)
    {
        _visitedPositions.Add(position);
    }

    public static void ResetVisitedPositions()
    {
        _visitedPositions.Clear();
    }

    public static HashSet<Position> VisitedPositions => _visitedPositions.ToHashSet();

    public static bool HasPlacedMineAt(Position position)
    {
        return MinePositions.Contains(position);
    }

    public static void PlaceMine(Position position)
    {
        MinePositions.Add(position);
    }

    public static void TriggerMine(Position position)
    {
        MinePositions.Remove(position);
    }

    public static HashSet<Position> GetPlacedMines()
    {
        return MinePositions.ToHashSet();
    }

    public static void Debug()
    {
        StringBuilder row = new StringBuilder();

        Player.Debug("possible mysubmarine map:");
        for (int y = 0; y < Map.Height; y++)
        {
            row.Clear();
            row.Append('|');
            for (int x = 0; x < Map.Width; x++)
            {
                if (_visitedPositions.Contains(new Position(x, y)))
                {
                    row.Append('X');
                }
                else
                {
                    row.Append(' ');
                }
            }
            row.Append('|');
            Player.Debug(row.ToString());
        }
        Player.Debug("---------------------------");
    }
}

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
            rows.Add( Console.ReadLine());
        }

        Map.InitializeMap(height, width, rows.ToArray());

        // Write an action using Console.WriteLine()
        // To debug: Console.Error.WriteLine("Debug messages...");

        var initialPosition = new StartingPositionComputer()
            .EvaluateBestPosition();
        Console.WriteLine(initialPosition.ToString());

        OpponentSubmarine.ResetPossiblePositions();

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

            var opponentOrders = Action.Parse(txtOpponentOrders);
            opponentOrders.Reverse();
            foreach (var action in opponentOrders)
            {
                OpponentSubmarine.EvaluateNewPossiblePositions(action);
            }
            
            OpponentSubmarine.Debug();

            inputs = line.Split(' ');
            int x = int.Parse(inputs[0]);
            int y = int.Parse(inputs[1]);

            var myPosition = new Position(x, y);

            MySubmarine.Visit(myPosition);

            int myLife = int.Parse(inputs[2]);
            int oppLife = int.Parse(inputs[3]);
            int torpedoCooldown = int.Parse(inputs[4]);
            int sonarCooldown = int.Parse(inputs[5]);
            int silenceCooldown = int.Parse(inputs[6]);
            int mineCooldown = int.Parse(inputs[7]);

            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");

            var gameState = new GameState(
                myPosition, opponentOrders,
                torpedoCooldown, sonarCooldown, silenceCooldown, mineCooldown);
            var ai = new AI(gameState);

            var actions = ai.ComputeActions();

            Console.WriteLine(Action.ToText(actions));
        }
    }

}