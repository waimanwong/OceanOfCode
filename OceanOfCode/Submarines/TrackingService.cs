using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

public class TrackingService
{
    private HashSet<Position> _possiblePositions = new HashSet<Position>();
    private int _health = 6;
    
    private MoveAction _lastMoveAction = null;

    public TrackingService(HashSet<Position> initialPositions)
    {
        _possiblePositions = initialPositions;
    }

    public HashSet<Position> PossiblePositions => _possiblePositions.ToHashSet();

    public void Track(MoveAction moveAction)
    {
        _lastMoveAction = moveAction;

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
        
        if (sector != -1)
        {   
            Player.Debug($"Enemy surfaces at sector {sector.ToString()}");

            var sectorPositions = Map.GetSectorWaterPositions(sector);

            var newPositions = _possiblePositions.Where(p => sectorPositions.Contains(p)).ToHashSet();

            _possiblePositions = newPositions;
        }

        _health--;

    }

    public void Track(TorpedoAction torpedoAction)
    {
        var torpedoPosition = torpedoAction.TargetPosition;
        
        var newPositions = _possiblePositions
            .Where(p => 1 <= p.DistanceTo(torpedoPosition) && p.DistanceTo(torpedoPosition) <= 4)
            .ToHashSet();

        _possiblePositions = newPositions;
    }

    public void Track(SilenceAction silenceAction)
    {
        var newPossiblePositions = new HashSet<Position>();

        var excludeDirection = GetOppositeDirection(_lastMoveAction.Direction);

        var possibleDirections = Player.FourDirectionDeltas.Where(x => x.Key != excludeDirection).ToList();

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

    public void Track(int newHealth, List<Action> opponentActions)
    {
        var lostHealtHCausedByWeapons = _health - newHealth;
        var weaponActions = opponentActions.OfType<IWeaponAction>();

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

            if (lostHealtHCausedByWeapons >= 2)
            {
                //Direct damage
                newPositions.Add(weaponPosition);
                lostHealtHCausedByWeapons -= 2;
            }
            else if( lostHealtHCausedByWeapons == 1)
            {
                foreach(var blastedPosition in blastedPositions)
                {
                    if (_possiblePositions.Contains(blastedPosition))
                        newPositions.Add(blastedPosition);
                }

                lostHealtHCausedByWeapons--;
            }
            else
            {
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
        }

        _health = newHealth;
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
                if(Map.IsIsland(x,y))
                {
                    row.Append(".");
                }
                else
                {
                    
                    if(_possiblePositions.Contains(new Position(x,y)))
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

    private Direction GetOppositeDirection(Direction direction)
    {
        switch (direction)
        {
            case Direction.E:
                return Direction.W;
            case Direction.N:
                return Direction.S;
            case Direction.S:
                return Direction.N;
            case Direction.W:
                return Direction.E;
            default:
                throw new NotSupportedException();
        }
    }

}
