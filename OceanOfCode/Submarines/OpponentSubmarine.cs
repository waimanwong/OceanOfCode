using System.Collections.Generic;
using System.Linq;

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
