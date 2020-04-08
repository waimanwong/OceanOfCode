using System;
using System.Linq;
using System.Collections.Generic;

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
