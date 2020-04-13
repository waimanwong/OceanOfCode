using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

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
