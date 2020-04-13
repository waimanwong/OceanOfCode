using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Collections;


 // LastEdited: 13/04/2020 13:16 



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

        var stealthScore = $"{MySubmarine.PossiblePositions.Count} - {OpponentSubmarine.PossiblePositions.Count}";
        actions.Add(new MessageAction(stealthScore));

        var selectedActions = SelectPowerActions();
        actions.AddRange(selectedActions);

        var selectedMoveAction = SelectMoveAction();
        actions.Add(selectedMoveAction);

        return actions;
    }

    private List<Action> SelectPowerActions()
    {
        var powerActions = new List<Action>();

        ////Silence ?
        if (TrySilence(out var silenceDirection, out var moves))
        {
            powerActions.Add(MySubmarine.Silence(silenceDirection, moves));
        }

        ////Place mine ?
        if (TrySelectMinePosition(out var position, out var mineDirection))
        {
            powerActions.Add(MySubmarine.PlaceMine((position, mineDirection)));
        }

        ////Trigger mine ?
        if (TryTriggerMine(out var minePosition))
        {
            powerActions.Add(MySubmarine.TriggerMine(minePosition));
        }

        ////Trigger torpedo ?
        if(TryLaunchTorpedo(out var torpedoPosition))
        {
            powerActions.Add(MySubmarine.LaunchTorpedo(torpedoPosition));
        }
                
        return powerActions;
    }

    private bool TryLaunchTorpedo(out Position torpedoPosition)
    {
        torpedoPosition = Position.None;

        if(_gameState.TorpedoAvailable == false)
            return false;

        var opponentPositions = OpponentSubmarine.PossiblePositions;
        if (opponentPositions.Count == 1)
        {
            var opponentPosition = opponentPositions.Single();

            if(opponentPosition.DistanceTo(MySubmarine.Position) <= 4)
            {
                torpedoPosition = opponentPosition;
            }
        }

        return torpedoPosition != Position.None;
    }

    private bool TryTriggerMine(out Position bestMinePosition)
    { 
        bestMinePosition = Position.None;

        var enemyPositions = OpponentSubmarine.PossiblePositions;

        if (enemyPositions.Count > 30)
        {
            return false;
        }

        //Select the mine which blast the maximum opponent positions
        var minePositions = MySubmarine.GetPlacedMines();
        var blastedOpponentPositions = 0;

        foreach(var minePosition in minePositions)
        {
            var blastedPositions = GetBlastedPositions(minePosition);

            if (blastedPositions.Contains(MySubmarine.Position) == false)
            {
                //Count how many possible positions are blasted
                var count = enemyPositions.Count(p => blastedPositions.Contains(p));

                if(count >= 6 || enemyPositions.Count <= 6)
                {
                    if (count > blastedOpponentPositions)
                    {
                        blastedOpponentPositions = count;
                        bestMinePosition = minePosition;
                    }
                }
            }
        }

        return bestMinePosition != Position.None;
    }

    private HashSet<Position> GetBlastedPositions(Position weaponPosition)
    {
        var blastedPositions = new List<Position>();

        blastedPositions.Add(weaponPosition);

        blastedPositions.AddRange( Player.EightDirectionDeltas
            .Select(delta => new Position(weaponPosition.x + delta.Item1, weaponPosition.y + delta.Item2))
            .Where(p => Map.IsWater(p))
            .ToList());

        return blastedPositions.ToHashSet();
    }

    private bool TrySilence(out Direction direction, out int moves)
    {
        direction = Direction.E;
        moves = 0;

        if(_gameState.SilenceAvailable == false)
        {
            return false;
        }

        List<(Direction,int)> possibleSilenceMoves = new List<(Direction, int)>();
        possibleSilenceMoves.Add((Direction.E, 0));

        foreach(var d in  Player.FourDirectionDeltas)
        {
            var curPos = MySubmarine.Position;
            var curDirection = d.Key;
            for(int move = 1; move <= 4; move++)
            {
                var deltaX = d.Value.Item1;
                var deltaY = d.Value.Item2;
                curPos = curPos.Translate(deltaX,deltaY);

                var notYetVisited = MySubmarine.VisitedPositions.Contains(curPos) == false;

                if(Map.IsWater(curPos) && notYetVisited)
                {
                    possibleSilenceMoves.Add((curDirection, move));
                }
                else
                {
                    //Can not go here and further, stop going in this direction
                    break;
                }
            }
        }

        var bestScore = -1;
        var bestSilenceMove = (Direction.E, 0);

        foreach(var currentSilenceMove in possibleSilenceMoves)
        {
            var myPossiblePositions = MySubmarine.TrackingService.PossiblePositions;
            var trackingService = new TrackingService(myPossiblePositions);

            trackingService.Track(new SilenceAction(currentSilenceMove.Item1, currentSilenceMove.Item2));

            var score = trackingService.PossiblePositions.Count;

            if(score > bestScore)
            {
                bestScore = score;
                bestSilenceMove = currentSilenceMove;
            }
        }

        direction = bestSilenceMove.Item1;
        moves = bestSilenceMove.Item2;

        return true;
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
        var neighborWaterPositions = Map.GetWaterNeighborPositions(myPosition);
        var placedMines = MySubmarine.GetPlacedMines();

        foreach (var item in neighborWaterPositions)
        {
            var possibleMinePosition = item.Item1;
            var possibleMineDirection = item.Item2;

            if (MySubmarine.HasPlacedMineAt(possibleMinePosition) == false)
            {
                var blastedPositions = GetBlastedPositions(possibleMinePosition);
                var blastOtherMines = blastedPositions.Any(p => placedMines.Contains(p));

                if (blastOtherMines == false)
                {
                    position = possibleMinePosition;
                    direction = possibleMineDirection;
                }
            }
        }

        return position != Position.None;
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

        var visitedPositions = MySubmarine.VisitedPositions;
        var rankedMoves = new Dictionary<int, List<Tuple<Position, Direction>>>();

        foreach (var possibleMove in possibleMoves)
        {   
            var floodFillEngine = new FloodFillEngine(visitedPositions);
            var filledRegion = floodFillEngine.Run(possibleMove.Item1);
            var score = filledRegion.Count;

            if(rankedMoves.ContainsKey(score) == false)
            {
                rankedMoves[score] = new List<Tuple<Position, Direction>>();
            }

            rankedMoves[score].Add(new Tuple<Position, Direction>(possibleMove.Item1, possibleMove.Item2));
        }

        var bestMoves = rankedMoves.OrderByDescending(kvp => kvp.Key).First().Value;
        var bestMove = GetBestMoveByStealth(bestMoves);
        
        return MySubmarine.MoveMySubmarine(bestMove, SelectPowerToCharge());
    }

    private (Position,Direction) GetBestMoveByStealth(List<Tuple<Position, Direction>> moves)
    {
        if(moves.Count == 1)
            return (moves.Single().Item1, moves.Single().Item2);

        var estimationOfMyPositions = MySubmarine.TrackingService.PossiblePositions;

        var bestScore = 0;
        var bestMove = moves.First();
        var iterations = 0;

        foreach(var move in moves)
        {
            var score = ScoreMove(move, MySubmarine.VisitedPositions, estimationOfMyPositions, iterations);

            if(score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return (bestMove.Item1, bestMove.Item2);
    }

    private int ScoreMove(
        Tuple<Position, Direction> move,
        HashSet<Position> visitedPositions, 
        HashSet<Position> estimationOfMyPositions, 
        int iterations)
    {
        var trackingService = new TrackingService(estimationOfMyPositions);
        var moveAction = new MoveAction(move.Item2, Power.UNKNOWN);
        trackingService.Track(moveAction);

        if(iterations == 0)
        {
            return trackingService.PossiblePositions.Count;
        }

        // next moves ?
        var curPosition = move.Item1;
        visitedPositions.Add(curPosition);
        
        var bestScore = -1;

        foreach(var directionDelta in Player.FourDirectionDeltas)
        {
            var deltaX = directionDelta.Value.Item1;
            var deltaY = directionDelta.Value.Item2;
            var newDirection = directionDelta.Key;
            var newPos = curPosition.Translate(deltaX, deltaY);
            if(Map.IsWater(newPos) && visitedPositions.Contains(newPos) == false)
            {
                var newEstimationOfMyPositions = trackingService.PossiblePositions;
                var score = ScoreMove(
                    new Tuple<Position, Direction>(newPos, newDirection), 
                    visitedPositions.ToHashSet(),
                    trackingService.PossiblePositions.ToHashSet(),
                    iterations - 1);

                if(score > bestScore)
                {
                    bestScore = score;
                }
            }
        }

        return bestScore;
    }

    private Power SelectPowerToCharge()
    {
        if (_gameState.TorpedoAvailable == false)
        {
            return Power.TORPEDO;
        }

        if(OpponentSubmarine.Health >= 2)
        {
            if (_gameState.MineAvailable == false)
            {
                return Power.MINE;
            }
        }

        if (_gameState.SilenceAvailable == false)
        {
            return Power.SILENCE;
        }

        // if (_gameState.SonarAvailable == false)
        // {
        //     return Power.SONAR;
        // }

        return Power.SILENCE;
    }

    private List<(Position, Direction)> GetPossibleDirectionsForMove(Position myPosition)
    {
        var visitedPositions = MySubmarine.VisitedPositions;

        var possibleDirections = new List<Direction>();
        var waterNeighborPositions = Map.GetWaterNeighborPositions(myPosition)
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

            var neighbors = Map.GetWaterNeighborPositions(currentPosition);

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

static class Map
{
    public static int Height;
    public static int Width;
    private static string[] Rows;

    private static char Water = '.';
    private static char Island = 'x';

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

    public static bool IsInMap(Position p)
    {
        return 0 <= p.x && p.x < Width &&
            0 <= p.y && p.y < Height;
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
        {  Direction.S, (0 ,  1) },
        {  Direction.W, (-1,  0) },
        {  Direction.E, (1 ,  0) },
        {  Direction.N, (0 , -1) },
    };

    public static (int, int)[] EightDirectionDeltas = new (int, int)[]
    {
        (-1,-1), (0, -1), (1, -1),
        (-1, 0),          (1,  0),
        (-1, 1), (0,  1), (1,  1)
    };

    public static Dictionary<Direction,Direction> OppositeDirection = new Dictionary<Direction, Direction>()
    {
        { Direction.E, Direction.W },
        { Direction.W, Direction.E },
        { Direction.N, Direction.S },
        { Direction.S, Direction.N },
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
            

            var gameState = new GameState(torpedoCooldown, sonarCooldown, silenceCooldown, mineCooldown);
            var ai = new AI(gameState);
            var actions = ai.ComputeActions();

            MySubmarine.ApplyActions(actions);            
            
            mylastActions = actions;

            var txtActions = Action.ToText(actions);

            Player.Debug($"{stopwatch.ElapsedMilliseconds - start} ms");

            Console.WriteLine(Action.ToText(actions));
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

public abstract class Action
{
    private static string _separator = "|";

    public static List<Action> Parse(string txtOpponentOrders)
    {
        var orders = txtOpponentOrders.Split(_separator.ToCharArray());

        var actions = new List<Action>();

        foreach (var order in orders)
        {
            var tokens = order.Trim().Split(' ');
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
public interface IWeaponAction
{
    Position TargetPosition { get; }
}
public class MessageAction : Action
{
    private readonly string _message;

    public MessageAction(string  message)
    {
        _message = message;
    }

    public override string ToString()
    {
        return $"MSG {_message}";
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

public class TorpedoAction : Action, IWeaponAction
{
    public static int Range = 4;
    public Position TargetPosition { get; }

    public TorpedoAction(Position position)
    {
        TargetPosition = position;
    }

    public override string ToString()
    {
        return $"TORPEDO {TargetPosition.ToString()}";
    }
}

public class TriggerAction : Action, IWeaponAction
{
    public Position TargetPosition { get; }
    
    public TriggerAction(Position position)
    {
        TargetPosition = position;
    }

    public override string ToString()
    {
        return $"TRIGGER {TargetPosition.ToString()}";
    }
}

public static class MySubmarine
{
    public static Position Position;

    public readonly static TrackingService TrackingService = new TrackingService(Map.WaterPositions);

    private static readonly HashSet<Position> _visitedPositions = new HashSet<Position>();

    public static HashSet<Position> PossiblePositions => TrackingService.PossiblePositions;

    private static readonly HashSet<Position> MinePositions = new HashSet<Position>();

    public static void UpdateState(int health, List<Action> myActions, List<Action> opponentActions)
    {
        TrackingService.TrackWeaponEffect(health, opponentActions.OfType<IWeaponAction>());
    }

    private static void MoveTo(Position position)
    {
        Position = position;

        _visitedPositions.Add(position);
    }

    public static void Debug()
    {
        TrackingService.Debug();
    }

    public static void ApplyActions(List<Action> actions)
    {
        foreach(var action in actions)
        {
            if (action is MoveAction)
            {
                TrackingService.Track((MoveAction)action);
                return;
            }

            if (action is SurfaceAction)
            {
                TrackingService.Track((SurfaceAction)action);
                return;
            }

            if (action is TorpedoAction)
            {
                TrackingService.Track((TorpedoAction)action);
                return;
            }

            if (action is SilenceAction)
            {
                TrackingService.Track((SilenceAction)action);
                return;
            }
        }
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

    public static TorpedoAction LaunchTorpedo(Position position)
    {
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
        _visitedPositions.Clear();
    
        MoveTo(Position);
        return new SurfaceAction();
    }

    public static TriggerAction TriggerMine(Position p)
    {
        MinePositions.Remove(p);

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

public static class OpponentSubmarine
{
    
    private static TrackingService _trackingService = new TrackingService(Map.WaterPositions);

    public static HashSet<Position> PossiblePositions => _trackingService.PossiblePositions;

    public static int Health => _trackingService.Health;

    public static void UpdateState(int newHealth, List<Action> opponentActions, List<Action> myActions)
    {
        //Play weaponActions
        var allWeaponActions = myActions.OfType<IWeaponAction>();

        _trackingService.TrackWeaponEffect(newHealth, allWeaponActions);

        //Then opponent actions
        opponentActions.ForEach(_trackingService.Track);
    }

    public static void Debug()
    {
        _trackingService.Debug();
    }

}

public class TrackingService
{
    private HashSet<Position> _possiblePositions = new HashSet<Position>();

    public int Health = -6;
    public int LostHealth = 0;

    private MoveAction _lastMoveAction = null;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="initialPositions">Positions where the submarine **can** be</param>
    public TrackingService(HashSet<Position> initialPositions)
    {
        _possiblePositions = initialPositions;
    }

    public HashSet<Position> PossiblePositions => _possiblePositions.ToHashSet();

    public void Track(Action submarineAction)
    {
        if (submarineAction is MoveAction)
        {   
            Track((MoveAction)submarineAction);
        }

        if (submarineAction is SurfaceAction)
        {
            Track((SurfaceAction)submarineAction);
        }

        if (submarineAction is TorpedoAction)
        {
            Track((TorpedoAction)submarineAction);
        }

        if (submarineAction is SilenceAction)
        {
            Track((SilenceAction)submarineAction);
        }        
    }

    private void Track(MoveAction moveAction)
    {
        var newPossiblePositions = new HashSet<Position>();

        foreach(var pos in _possiblePositions)
        {
            var direction = moveAction.Direction;
            var delta = Player.FourDirectionDeltas[direction];
            var newPos = pos.Translate(delta.Item1, delta.Item2);
            
            if(Map.IsWater(newPos))
            {
                newPossiblePositions.Add(newPos);
            }
        }

        _lastMoveAction = moveAction;
        _possiblePositions = newPossiblePositions;
    }

    private void Track(SurfaceAction surfaceAction)
    {
        var sector = surfaceAction.sector;
        
        if (sector != -1)
        {   
            Player.Debug($"Enemy surfaces at sector {sector.ToString()}");

            var sectorPositions = Map.GetSectorWaterPositions(sector);

            var newPositions = _possiblePositions.Where(p => sectorPositions.Contains(p)).ToHashSet();

            _possiblePositions = newPositions;
        }

        Health--;
    }

    private void Track(TorpedoAction torpedoAction)
    {
        var torpedoPosition = torpedoAction.TargetPosition;
        
        var newPositions = _possiblePositions
            .Where(p => 1 <= p.DistanceTo(torpedoPosition) && p.DistanceTo(torpedoPosition) <= 4)
            .ToHashSet();

        _possiblePositions = newPositions;
    }

    private void Track(SilenceAction silenceAction)
    {
        var newPossiblePositions = new HashSet<Position>();

        var possibleDirections = Player.FourDirectionDeltas.ToList();

        if(_lastMoveAction != null)
        {
            var excludeDirection = Player.OppositeDirection[_lastMoveAction.Direction];
            possibleDirections = possibleDirections.Where(x => x.Key != excludeDirection).ToList();
        }

        foreach (var pos in _possiblePositions)
        {
            foreach(var direction in possibleDirections)
            {
                for(int move = 0; move <= 4; move++)
                {
                    var delta = direction.Value;
                    var newPos = pos.Translate(delta.Item1 * move, delta.Item2 * move);

                    if (Map.IsWater(newPos))
                    {
                        newPossiblePositions.Add(newPos);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        _possiblePositions = newPossiblePositions;
    }

    public void TrackWeaponEffect(int newHealth, IEnumerable<IWeaponAction> weaponActions)
    {
        LostHealth = Health - newHealth;
        Health = newHealth;

        if(weaponActions.Count() == 0)
        {
            return;
        }

        var newPositions = new HashSet<Position>();

        foreach (var weaponAction in weaponActions)
        {
            var weaponPosition = weaponAction.TargetPosition;
            var blastedPositions = Player.EightDirectionDeltas
                    .Select(delta => new Position(weaponPosition.x + delta.Item1, weaponPosition.y + delta.Item2))
                    .ToList();

            if(LostHealth == 0)
            {
                //No damage, remove possibilities
                foreach(var position in _possiblePositions)
                {
                    var positionIsNotBlasted = position != weaponPosition &&
                            blastedPositions.Contains(position) == false;
                    if(positionIsNotBlasted)
                    {
                        newPositions.Add(position);
                    }
                }
            }
            else if (LostHealth == 2)
            {
                //Direct damage
                if(_possiblePositions.Contains(weaponPosition))
                    newPositions.Add(weaponPosition);
            }
            else 
            {
                foreach(var blastedPosition in blastedPositions)
                {
                    if (_possiblePositions.Contains(blastedPosition))
                        newPositions.Add(blastedPosition);
                }
            }
        }
        
        //in case algo is wrong, ignore it
        if(newPositions.Count > 0)
            _possiblePositions = newPositions;
    }

    public void Debug()
    {
        Player.Debug($"possible positions: {_possiblePositions.Count}");

        var row = new StringBuilder();
        for (int y=0; y < Map.Height; y++)
        {
            row.Clear();
            row.Append('|');
            for(int x = 0; x < Map.Width; x++)
            {
                var position = new Position(x,y);

                if(Map.IsWater(position) == false)
                {
                    row.Append(".");
                }
                else
                {   
                    if(_possiblePositions.Contains(position))
                    {
                        row.Append("?");
                    }
                    else
                    {
                        row.Append(" ");
                    }
                }
            }
            row.Append('|');
            Player.Debug(row.ToString());
        }

    }

    

}