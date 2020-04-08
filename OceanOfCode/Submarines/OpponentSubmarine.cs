using System.Collections.Generic;

public static class OpponentSubmarine
{
    private static int _health = 6;

    private static TrackingService _trackingService = new TrackingService(Map.WaterPositions);

    public static HashSet<Position> PossiblePositions => _trackingService.PossiblePositions;

    public static void UpdateState(int newHealth, string txtOpponentOrders)
    {
        var opponentOrders = Action.Parse(txtOpponentOrders);
       
        foreach (var action in opponentOrders)
        {
            OpponentSubmarine.ApplyAction(action);
        }
    }

    public static void Debug()
    {
        _trackingService.Debug();
    }

    private static void ApplyAction(Action action)
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
}
