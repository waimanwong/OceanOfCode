using System.Collections.Generic;
using System.Linq;

public static class MySubmarine
{
    public static Position Position;

    private static TrackingService _trackingService = new TrackingService(Map.WaterPositions);

    private static readonly HashSet<Position> _visitedPositions = new HashSet<Position>();

    public static readonly HashSet<Position> MinePositions = new HashSet<Position>();

    private static void MoveTo(Position position)
    {
        Position = position;

        _visitedPositions.Add(position);
    }

    public static void Debug()
    {
        _trackingService.Debug();
    }

    public static void ApplyAction(Action action)
    {
        if (action is MoveAction)
        {
            _trackingService.Track((MoveAction)action);
            return;
        }

        if (action is SurfaceAction)
        {
            _trackingService.Track((SurfaceAction)action);
            return;
        }

        if (action is TorpedoAction)
        {
            _trackingService.Track((TorpedoAction)action);
            return;
        }

        if (action is SilenceAction)
        {
            _trackingService.Track((SilenceAction)action);
            return;
        }

        return;
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
