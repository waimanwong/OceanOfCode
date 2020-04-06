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

        for (int y = 0; y < height; y++)
        {
            PossibleMoveCount[y] = new int[width];

            for (int x = 0; x < width; x++)
            {
                var position = new Position(x, y);

                if (IsWater(position))
                {
                    var possibleMoveCount = Map.GetNeighborPositions(position)
                        .Count(neighpos => IsWater(neighpos.Item1));

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
        var (x, y) = (coord.x, coord.y);

        return IsWater(x, y);
    }

    public static bool IsWater(int x, int y)
    {
        return (0 <= x && x < Width) &&
            (0 <= y && y < Height) &&
            Rows[y][x] == Water;
    }


    public static bool IsIsland(Position coord)
    {
        var (x, y) = (coord.x, coord.y);

        return IsIsland(x, y);
    }

    public static bool IsIsland(int x, int y)
    {
        return (0 <= x && x < Width) &&
            (0 <= y && y < Height) &&
            Rows[y][x] == Island;
    }

    /// <summary>
    /// Returns neighbors positions whether map or land
    /// </summary>
    /// <param name="fromPosition"></param>
    /// <returns></returns>
    public static List<(Position, Direction)> GetNeighborPositions(Position fromPosition)
    {
        var neighborPositions = new List<(Position, Direction)>(4);
        foreach (var direction in Player.AllDirections)
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
        while (Map.IsWater(currentPosition) == false)
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

        foreach (var position in randomPositions)
        {
            if (bestFilledRegion.Contains(position) == true)
            {
                //Do nothings
            }
            else
            {
                var noVisitedPositions = new HashSet<Position>();
                var fillEngine = new FloodFillEngine(noVisitedPositions);
                var filledRegion = fillEngine.Run(position);

                if (filledRegion.Count > bestFilledRegion.Count)
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
    private readonly int _torpedoCooldown;
    private readonly int _sonarCooldown;
    private readonly int _silenceCooldown;
    private readonly int _mineCooldown;

    public GameState(int torpedoCooldown, int sonarCooldown, int silenceCooldown, int mineCooldown)
    {
        _torpedoCooldown = torpedoCooldown;
        _sonarCooldown = sonarCooldown;
        _silenceCooldown = silenceCooldown;
        _mineCooldown = mineCooldown;
    }

    public bool TorpedoAvailable => _torpedoCooldown == 0;
    public bool SonarAvailable => _sonarCooldown == 0;
    public bool SilenceAvailable => _silenceCooldown == 0;
    public bool MineAvailable => _mineCooldown == 0;

}

public enum Direction { N, S, E, W }

public enum Power { UNKNOWN, TORPEDO, SILENCE, SONAR, MINE }

#region actions
public abstract class Action
{
    private static string _separator = "|";

    public static List<Action> Parse(string txtOpponentOrders)
    {
        var orders = txtOpponentOrders.Split(_separator.ToCharArray());

        var actions = new List<Action>();

        foreach (var order in orders)
        {
            var tokens = order.Split(' ');
            var cmd = tokens[0];
            switch (cmd)
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

public class MoveAction : Action
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

public class SurfaceAction : Action
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

public class TorpedoAction : Action
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

public class SonarAction : Action
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

public class SilenceAction : Action
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

public class MineAction : Action
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

public class TriggerAction : Action
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

        var selectedActions = SelectPowerActions();
        actions.AddRange(selectedActions);

        var selectedMoveAction = SelectMoveAction();
        actions.Add(selectedMoveAction);

        return actions;
    }

    private List<Action> SelectPowerActions()
    {
        var powerActions = new List<Action>();

        //Silence ?
        if (TrySilence(out var silenceDirection, out var moves))
        {
            powerActions.Add(MySubmarine.Silence(silenceDirection, moves));
        }

        //Place mine ?
        if (TrySelectMinePosition(out var position, out var mineDirection))
        {
            powerActions.Add(MySubmarine.PlaceMine((position, mineDirection)));
        }

        //Trigger mine ?
        if (TryTriggerMine(out var minePosition))
        {
            powerActions.Add(MySubmarine.TriggerMine(minePosition));
        }

        if (TryLaunchTorpedo(out var torpedoPosition))
        {
            powerActions.Add(MySubmarine.LaunchTorpedo(torpedoPosition));
        }

        return powerActions;
    }

    private bool TryLaunchTorpedo(out Position torpedoPosition)
    {
        torpedoPosition = Position.None;

        if (_gameState.TorpedoAvailable == false)
        {
            return false;
        }

        var myPosition = MySubmarine.Position;
        var enemyPositionInRange = OpponentSubmarine.PossiblePositions
            .Where(p => p.DistanceTo(myPosition) <= 4 && p.DistanceTo(myPosition) > 1)
            .OrderByDescending(p => p.DistanceTo(myPosition))
            .ToList();

        foreach (var position in enemyPositionInRange)
        {
            var blastedPosition = Player.EightDirectionDeltas
                .Select(delta => new Position(delta.Item1, delta.Item2))
                .ToHashSet();

            if (blastedPosition.Contains(myPosition) == false)
            {
                torpedoPosition = position;
                break;
            }
        }

        return torpedoPosition != Position.None;
    }

    private bool TrySilence(out Direction direction, out int moves)
    {
        direction = Direction.E;
        moves = 0;

        if (_gameState.SilenceAvailable == false)
        {
            return false;
        }

        var myPosition = MySubmarine.Position;

        var possibleSilenceMoves = new HashSet<(Direction, int)>();

        foreach (var kvp in Player.FourDirectionDeltas)
        {
            var d = kvp.Key;
            var delta = kvp.Value;

            var currentPosition = myPosition;
            for (int step = 1; step <= 4; step++)
            {
                currentPosition = currentPosition.Translate(delta.Item1, delta.Item2);
                if (Map.IsWater(currentPosition) && MySubmarine.VisitedPositions.Contains(currentPosition) == false)
                {
                    //ok
                    possibleSilenceMoves.Add((d, step));
                }
                else
                {
                    break;
                }
            }
        }

        if (possibleSilenceMoves.Count == 0)
        {
            return false;
        }

        var random = new Random((int)Stopwatch.GetTimestamp());
        var selectedIndex = random.Next(0, possibleSilenceMoves.Count - 1);

        var selectedMove = possibleSilenceMoves.ElementAt(selectedIndex);
        direction = selectedMove.Item1;
        moves = selectedMove.Item2;
        return true;
    }

    public bool TryTriggerMine(out Position position)
    {
        position = Position.None;

        var placedMines = MySubmarine.GetPlacedMines();

        if (placedMines.Count == 0)
        {
            return false;
        }

        var myPosition = MySubmarine.Position;
        var bestMineToTrigger = Position.None;
        var enemyPossiblePositions = OpponentSubmarine.PossiblePositionCount;
        var bestScore = int.MaxValue;

        foreach (var minePosition in placedMines)
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
            foreach (var blastedPosition in blastedPositions)
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

        position = bestMineToTrigger;

        return position != Position.None;
    }

    private bool TrySelectMinePosition(out Position position, out Direction direction)
    {
        position = Position.None;
        direction = Direction.E;

        if (_gameState.MineAvailable == false)
        {
            return false;
        }

        var myPosition = MySubmarine.Position;
        var neighborWaterPositions = Map.GetNeighborPositions(myPosition)
            .Where(x => Map.IsWater(x.Item1))
            .ToList();

        int maxCoverage = -1;

        foreach (var item in neighborWaterPositions)
        {
            var possibleMinePosition = item.Item1;
            var possibleMineDirection = item.Item2;

            if (MySubmarine.HasPlacedMineAt(possibleMinePosition) == false)
            {
                //Maximize area of blast
                var newMines = MySubmarine.GetPlacedMines();
                newMines.Add(possibleMinePosition);

                var coverage = ComputeCoveredAreaByMines(newMines);

                Player.Debug($"Mine at {possibleMineDirection.ToString()}, coverage = {coverage.ToString()}");

                var coverageIsBetter = coverage > maxCoverage;

                if (coverageIsBetter)
                {
                    maxCoverage = coverage;
                    position = possibleMinePosition;
                    direction = possibleMineDirection;
                }
            }
        }

        return maxCoverage > 0;
    }

    private static int ComputeCoveredAreaByMines(HashSet<Position> minePositions)
    {
        var blastedPositions = new HashSet<Position>();

        foreach (var mine in minePositions)
        {
            foreach (var delta in Player.EightDirectionDeltas)
            {
                var blastedPosition = new Position(mine.x + delta.Item1, mine.y + delta.Item2);
                if (Map.IsInMap(blastedPosition) && Map.IsWater(blastedPosition))
                {
                    blastedPositions.Add(blastedPosition);
                }
            }
        }

        return blastedPositions.Count;
    }

    private Action SelectMoveAction()
    {
        var fromPosition = MySubmarine.Position;
        var possibleMoves = GetPossibleDirectionsForMove(fromPosition);
        var possibleMoveCount = possibleMoves.Count;

        if (possibleMoveCount == 0)
        {
            return MySubmarine.SurfaceMySubmarine();
        }

        if (possibleMoveCount == 1)
        {
            var possibleMove = possibleMoves.Single();
            return MySubmarine.MoveMySubmarine(possibleMove, SelectPowerToCharge());
        }

        var bestMove = possibleMoves.First();
        var bestScore = 0;
        var bestFilledRegion = new HashSet<Position>();
        var visitedPositions = MySubmarine.VisitedPositions;

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

        return MySubmarine.MoveMySubmarine(bestMove, SelectPowerToCharge());
    }

    private Power SelectPowerToCharge()
    {
        if (_gameState.TorpedoAvailable == false)
        {
            return Power.TORPEDO;
        }

        if (_gameState.SilenceAvailable == false)
        {
            return Power.SILENCE;
        }

        if (_gameState.MineAvailable == false)
        {
            return Power.MINE;
        }

        if (_gameState.SonarAvailable == false)
        {
            return Power.SONAR;
        }

        return Power.MINE;
    }

    private List<(Position, Direction)> GetPossibleDirectionsForMove(Position myPosition)
    {
        var visitedPositions = MySubmarine.VisitedPositions;

        var possibleDirections = new List<Direction>();
        var waterNeighborPositions = Map.GetNeighborPositions(myPosition)
            .Where(x => Map.IsWater(x.Item1))
            .Where(x => visitedPositions.Contains(x.Item1) == false);

        return waterNeighborPositions.ToList();
    }

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

        while (q.Count > 0)
        {
            var currentPosition = q.Dequeue();

            var neighbors = Map.GetNeighborPositions(currentPosition)
                .Where(x => Map.IsWater(x.Item1))
                .ToList();

            foreach (var neighbor in neighbors)
            {
                if (_alreadyVisitedPositions.Contains(neighbor.Item1) == false)
                {
                    if (_remainingPositionsToVisit.Contains(neighbor.Item1) == false)
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

    public static void UpdateState(Action opponentAction)
    {
        if (opponentAction is MoveAction)
        {
            MoveAction moveAction = (MoveAction)opponentAction;
            var (dx, dy) = Player.FourDirectionDeltas[moveAction.Direction];

            //Add possible positions
            var currentPossiblePositions = _possiblePositions.ToHashSet();
            foreach (var currentPosition in currentPossiblePositions)
            {
                var newPosition = currentPosition.Translate(dx, dy);
                if (Map.IsInMap(newPosition) && Map.IsWater(newPosition))
                    _possiblePositions.Add(newPosition);
            }

            //Remove impossible positions
            _possiblePositions = _possiblePositions
                .Where(p => {
                    var previousPosition = p.Translate(-dx, -dy);
                    return Map.IsWater(previousPosition) && _possiblePositions.Contains(previousPosition);
                })
                .ToHashSet();

            return;
        }

        if (opponentAction is SurfaceAction)
        {
            ResetPossiblePositions();

            SurfaceAction surfaceAction = (SurfaceAction)opponentAction;

            var (topLeftPosition, bottomRigthPosition) = Map.GetSectorBounds(surfaceAction.sector);
            _possiblePositions = _possiblePositions
                .Where(p => (topLeftPosition.x <= p.x && p.x <= bottomRigthPosition.x) &&
                           (topLeftPosition.y <= p.y && p.y <= bottomRigthPosition.y))
                .ToHashSet();

            return;
        }

        if (opponentAction is TorpedoAction)
        {
            TorpedoAction torpedoAction = (TorpedoAction)opponentAction;
            _possiblePositions = _possiblePositions
                .Where(p => p.DistanceTo(torpedoAction.TargetPosition) <= 4)
                .ToHashSet();

            return;
        }

        if (opponentAction is SilenceAction)
        {
            var possiblePositions = PossiblePositions;
            foreach (var position in possiblePositions)
            {
                foreach (var direction in Player.FourDirectionDeltas)
                {
                    for (int i = 1; i <= 4; i++)
                    {
                        var deltaX = direction.Value.Item1;
                        var deltaY = direction.Value.Item2;

                        var p = position.Translate(i * deltaX, i * deltaY);
                        if (Map.IsWater(p))
                        {
                            _possiblePositions.Add(p);
                        }
                    }
                }
            }
            return;
        }

        return;
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
                if (_possiblePositions.Contains(new Position(x, y)))
                {
                    row.Append(' ');
                }
                else
                {
                    if (Map.IsIsland(x, y))
                    {
                        row.Append('.');
                    }
                    else
                    {
                        row.Append('X');
                    }
                }
            }
            row.Append('|');
            Player.Debug(row.ToString());
        }
        Player.Debug("---------------------------");
    }

    public static void UpdateStateCausedByMyWeapons(int lostHealthCausedByWeapon)
    {
        if (lostHealthCausedByWeapon == 0)
        {
            foreach (var position in MySubmarine.JustTriggeredWeapons)
            {
                _possiblePositions.Remove(position);

                var blastedPositions = Player.EightDirectionDeltas.Select(x => position.Translate(x.Item1, x.Item2));

                foreach (var blastedPosition in blastedPositions)
                {
                    _possiblePositions.Remove(blastedPosition);
                }
            }

            return;
        }

        _possiblePositions.Clear();
        foreach (var position in MySubmarine.JustTriggeredWeapons)
        {
            if (lostHealthCausedByWeapon == 2)
            {
                _possiblePositions.Add(position);
            }
            else
            {
                var blastedPositions = Player.EightDirectionDeltas.Select(x => position.Translate(x.Item1, x.Item2));
                foreach (var blastedPosition in blastedPositions)
                {
                    if (Map.IsWater(blastedPosition))
                        _possiblePositions.Add(blastedPosition);
                }
            }
        }
    }

    public static void Apply(int newHealth, string txtOpponentOrders)
    {
        var opponentOrders = Action.Parse(txtOpponentOrders);
        var lostHealthCausedByWeapon = _health - newHealth;

        if (opponentOrders.OfType<SurfaceAction>().Any())
        {
            lostHealthCausedByWeapon = lostHealthCausedByWeapon - 1;
        }

        OpponentSubmarine.UpdateStateCausedByMyWeapons(lostHealthCausedByWeapon);

        foreach (var action in opponentOrders)
        {
            OpponentSubmarine.UpdateState(action);
        }

        _health = newHealth;
    }
}

public static class MySubmarine
{
    public static Position Position;

    private static readonly HashSet<Position> _visitedPositions = new HashSet<Position>();

    public static readonly HashSet<Position> MinePositions = new HashSet<Position>();

    private static void MoveTo(Position position)
    {
        Position = position;

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
                if (Position.x == x && Position.y == y)
                {
                    row.Append('M');
                }
                else
                {
                    if (_visitedPositions.Contains(new Position(x, y)))
                    {
                        row.Append('X');
                    }
                    else
                    {
                        if (Map.IsIsland(x, y))
                        {
                            row.Append('.');
                        }
                        else
                        {
                            row.Append(' ');
                        }
                    }
                }
            }
            row.Append('|');
            Player.Debug(row.ToString());
        }
        Player.Debug("---------------------------");
    }

    public static List<Position> JustTriggeredWeapons = new List<Position>();

    public static TorpedoAction LaunchTorpedo(Position position)
    {
        JustTriggeredWeapons.Add(position);

        return new TorpedoAction(position);
    }

    public static MineAction PlaceMine((Position, Direction) place)
    {
        MinePositions.Add(place.Item1);

        return new MineAction(place.Item2);
    }

    public static MoveAction MoveMySubmarine((Position, Direction) move, Power power)
    {
        var newPosition = move.Item1;

        MoveTo(newPosition);

        return new MoveAction(move.Item2, power);
    }

    public static SurfaceAction SurfaceMySubmarine()
    {
        ResetVisitedPositions();

        return new SurfaceAction();
    }

    public static TriggerAction TriggerMine(Position p)
    {
        MinePositions.Remove(p);

        JustTriggeredWeapons.Add(p);

        return new TriggerAction(p);
    }

    public static SilenceAction Silence(Direction direction, int moves)
    {
        var delta = Player.FourDirectionDeltas[direction];
        for (int i = 0; i < moves; i++)
        {
            MoveTo(Position.Translate(delta.Item1, delta.Item2));
        }

        return new SilenceAction(direction, moves);
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
            rows.Add(Console.ReadLine());
        }

        Map.InitializeMap(height, width, rows.ToArray());

        // Write an action using Console.WriteLine()
        // To debug: Console.Error.WriteLine("Debug messages...");

        var initialPosition = new StartingPositionComputer()
            .EvaluateBestPosition();
        Console.WriteLine(initialPosition.ToString());

        MySubmarine.MoveMySubmarine((initialPosition, Direction.E), Power.MINE);
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

            OpponentSubmarine.Apply(oppLife, txtOpponentOrders);
            OpponentSubmarine.Debug();

            MySubmarine.JustTriggeredWeapons.Clear();

            var gameState = new GameState(torpedoCooldown, sonarCooldown, silenceCooldown, mineCooldown);

            var ai = new AI(gameState);

            var actions = ai.ComputeActions();

            Console.WriteLine(Action.ToText(actions));
        }
    }

}