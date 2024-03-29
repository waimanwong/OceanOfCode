﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

public class TrackingService
{
    private HashSet<Position> _possiblePositions = new HashSet<Position>();
    public int Health = -6;
    public int LostHealth = 0;
    public MoveAction LastMoveAction = null;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="initialPositions">Positions where the submarine **can** be</param>
    public TrackingService(HashSet<Position> initialPositions, MoveAction lastMoveAction)
    {
        _possiblePositions = initialPositions;
        LastMoveAction = lastMoveAction;
    }

    public HashSet<Position> PossiblePositions => _possiblePositions.ToHashSet();

    public void Track(Action submarineAction)
    {
        if (submarineAction is MoveAction)
        {   
            TrackMoveAction((MoveAction)submarineAction);
        }

        if (submarineAction is SurfaceAction)
        {
            TrackSurfaceAction((SurfaceAction)submarineAction);
        }

        if (submarineAction is TorpedoAction)
        {
            TrackTorpedoAction((TorpedoAction)submarineAction);
        }

        if (submarineAction is SilenceAction)
        {
            TrackSilenceAction();
        }        
    }

    private void TrackMoveAction(MoveAction moveAction)
    {
        var newPossiblePositions = new HashSet<Position>();
        var direction = moveAction.Direction;

        foreach(var pos in _possiblePositions)
        {
            if(Map.TryGetNeighborPosition(pos, direction, out var newPos))
                newPossiblePositions.Add(newPos);
        }

        LastMoveAction = moveAction;
        _possiblePositions = newPossiblePositions;
    }

    private void TrackSurfaceAction(SurfaceAction surfaceAction)
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

    private void TrackTorpedoAction(TorpedoAction torpedoAction)
    {
        var torpedoPosition = torpedoAction.TargetPosition;
        
        var newPositions = _possiblePositions
            .Where(p => 1 <= p.DistanceTo(torpedoPosition) && p.DistanceTo(torpedoPosition) <= 4)
            .ToHashSet();

        _possiblePositions = newPositions;
    }

    private void TrackSilenceAction()
    {
        var newPossiblePositions = new HashSet<Position>();
        var visitedPositions = new HashSet<Position>();

        var excludeDirection = Player.OppositeDirection[LastMoveAction.Direction];

        foreach (var pos in _possiblePositions)
        {
            var possibleSilenceActions = SilenceAction.ComputeSilenceActions(pos, visitedPositions )
                .Where(silenceAction => silenceAction.Item1 != excludeDirection)
                .ToList();
            
            foreach(var possibleSilenceAction in possibleSilenceActions)
            {
                newPossiblePositions.Add(possibleSilenceAction.Item3);
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
            var blastedPositions = Map.NeighborBlastedPositions[weaponPosition].ToHashSet();

            if(LostHealth == 0)
            {
                //No damage, remove possibilities
                foreach(var position in _possiblePositions)
                {
                    var positionIsNotBlasted = position.IsNot( weaponPosition) &&
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
