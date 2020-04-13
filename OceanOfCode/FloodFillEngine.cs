using System.Linq;
using System.Collections.Generic;

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

            var neighborPositions = Map.GetNeighborPositions(currentPosition).Values;

            foreach (var neighborPosition in neighborPositions)
            {
                if (_alreadyVisitedPositions.Contains(neighborPosition) == false)
                {
                    if (_remainingPositionsToVisit.Contains(neighborPosition) == false)
                    {
                        _remainingPositionsToVisit.Add(neighborPosition);
                        q.Enqueue(neighborPosition);
                    }
                }

            }
        }

        return _remainingPositionsToVisit;
    }
}
