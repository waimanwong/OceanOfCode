using System;
using System.Linq;
using System.Collections.Generic;

public class TrackingService
{
    private HashSet<Position> _possiblePositions = new HashSet<Position>();

    public HashSet<Position> PossiblePositions => _possiblePositions.ToHashSet();

    public TrackingService(HashSet<Position> initialPositions)
    {
        _possiblePositions = initialPositions;
    }

    public void Track(MoveAction moveAction)
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

        _possiblePositions = newPossiblePositions;
    }

    public void Track(SurfaceAction surfaceAction)
    {
        var sector = surfaceAction.sector;
        var sectorPositions = Map.GetSectorWaterPositions(sector);

        var newPositions = _possiblePositions.Where(p => sectorPositions.Contains(p)).ToHashSet();

        _possiblePositions = sectorPositions;
    }

    public void Track(TorpedoAction torpedoAction)
    {
        var torpedoPosition = torpedoAction.TargetPosition;
        var area = Map.WaterPositions.Where(p => p.DistanceTo(torpedoPosition) <= 4);
        var newPositions = _possiblePositions.Where(p => area.Contains(p)).ToHashSet();

        _possiblePositions = newPositions;
    }

    public void Track(SilenceAction silenceAction)
    {
        var newPossiblePositions = new HashSet<Position>();

        foreach (var pos in _possiblePositions)
        {
            foreach(var direction in Player.FourDirectionDeltas)
            {
                for(int move = 0; move <= 4; move++)
                {
                    var delta = direction.Value;
                    var newPos = pos.Translate(delta.Item1 * move, delta.Item2 * move);

                    if (Map.IsWater(newPos))
                    {
                        newPossiblePositions.Add(newPos);
                    }
                }
            }
        }

        _possiblePositions = newPossiblePositions;
    }

    public void Debug()
    {
        Player.Debug($"possible positions: {_possiblePositions.Count}");
        if(_possiblePositions.Count < 10)
        {
            var text = string.Join(",",_possiblePositions.Select(p => $"({p.ToString()})").ToArray());
            Player.Debug(text);
        }
    }
}
