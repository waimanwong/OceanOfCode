﻿using System.Collections.Generic;

public static class OpponentSubmarine
{
    private static int _health = 6;

    private static TrackingService _trackingService = new TrackingService(Map.WaterPositions);

    public static HashSet<Position> PossiblePositions => _trackingService.PossiblePositions;

    public static void UpdateState(int newHealth, string txtOpponentOrders, List<Action> myActions)
    {
        //Play first my actions
        _trackingService.Track(newHealth, myActions);

        //Then opponent actions
        var opponentOrders = Action.Parse(txtOpponentOrders);
       
        foreach (var action in opponentOrders)
        {
            OpponentSubmarine.ApplyAction(action);

            Player.Debug($"after processing {action.ToString()}");
            Debug();

        }
    }

    public static void Debug()
    {
        Player.Debug("*********************************************");
        Player.Debug("Opponent submarine data");
        _trackingService.Debug();
        Player.Debug("*********************************************");
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
