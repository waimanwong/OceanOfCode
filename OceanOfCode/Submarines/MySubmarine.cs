using System.Collections.Generic;
using System.Linq;

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
