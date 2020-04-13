﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

class AI
{
    private readonly GameState _gameState;

    public AI(GameState gameState)
    {
        _gameState = gameState;
    }

    public List<Action> ComputeActions()
    {
        var actions = new List<Action>();

        var stealthScore = $"{MySubmarine.PossiblePositions.Count} - {OpponentSubmarine.PossiblePositions.Count}";
        actions.Add(new MessageAction(stealthScore));

        var selectedActions = SelectPowerActions();
        actions.AddRange(selectedActions);

        var selectedMoveAction = SelectMoveAction();
        actions.Add(selectedMoveAction);

        return actions;
    }

    private List<Action> SelectPowerActions()
    {
        var powerActions = new List<Action>();

        ////Silence ?
        if (TrySilence(out var silenceDirection, out var moves))
        {
            powerActions.Add(MySubmarine.Silence(silenceDirection, moves));
        }

        ////Place mine ?
        if (TrySelectMinePosition(out var position, out var mineDirection))
        {
            powerActions.Add(MySubmarine.PlaceMine((position, mineDirection)));
        }

        ////Trigger mine ?
        if (TryTriggerMine(out var minePosition))
        {
            powerActions.Add(MySubmarine.TriggerMine(minePosition));
        }

        ////Trigger torpedo ?
        if(TryLaunchTorpedo(out var torpedoPosition))
        {
            powerActions.Add(MySubmarine.LaunchTorpedo(torpedoPosition));
        }
                
        return powerActions;
    }

    private bool TryLaunchTorpedo(out Position torpedoPosition)
    {
        torpedoPosition = Position.None;

        if(_gameState.TorpedoAvailable == false)
            return false;

        var opponentPositions = OpponentSubmarine.PossiblePositions;
        if (opponentPositions.Count == 1)
        {
            var opponentPosition = opponentPositions.Single();

            if(opponentPosition.DistanceTo(MySubmarine.Position) <= 4)
            {
                torpedoPosition = opponentPosition;
            }
        }

        return torpedoPosition .IsNot(Position.None);
    }

    private bool TryTriggerMine(out Position bestMinePosition)
    { 
        bestMinePosition = Position.None;

        var enemyPositions = OpponentSubmarine.PossiblePositions;

        if (enemyPositions.Count > 30)
        {
            return false;
        }

        //Select the mine which blast the maximum opponent positions
        var minePositions = MySubmarine.GetPlacedMines();
        var blastedOpponentPositions = 0;

        foreach(var minePosition in minePositions)
        {
            var blastedPositions = GetBlastedPositions(minePosition);

            if (blastedPositions.Contains(MySubmarine.Position) == false)
            {
                //Count how many possible positions are blasted
                var count = enemyPositions.Count(p => blastedPositions.Contains(p));

                if(count >= 6 || enemyPositions.Count <= 6)
                {
                    if (count > blastedOpponentPositions)
                    {
                        blastedOpponentPositions = count;
                        bestMinePosition = minePosition;
                    }
                }
            }
        }

        return bestMinePosition.IsNot( Position.None);
    }

    private HashSet<Position> GetBlastedPositions(Position weaponPosition)
    {
        var blastedPositions = new List<Position>();

        blastedPositions.Add(weaponPosition);

        blastedPositions.AddRange( Player.EightDirectionDeltas
            .Select(delta => new Position(weaponPosition.x + delta.Item1, weaponPosition.y + delta.Item2))
            .Where(p => Map.IsWater(p))
            .ToList());

        return blastedPositions.ToHashSet();
    }

    private bool TrySilence(out Direction direction, out int moves)
    {
        direction = Direction.E;
        moves = 0;

        if(_gameState.SilenceAvailable == false)
        {
            return false;
        }

        List<(Direction,int)> possibleSilenceMoves = new List<(Direction, int)>();
        possibleSilenceMoves.Add((Direction.E, 0));

        foreach(var d in  Player.FourDirectionDeltas)
        {
            var curPos = MySubmarine.Position;
            var curDirection = d.Key;
            for(int move = 1; move <= 4; move++)
            {
                var deltaX = d.Value.Item1;
                var deltaY = d.Value.Item2;
                curPos = curPos.Translate(deltaX,deltaY);

                var notYetVisited = MySubmarine.VisitedPositions.Contains(curPos) == false;

                if(Map.IsWater(curPos) && notYetVisited)
                {
                    possibleSilenceMoves.Add((curDirection, move));
                }
                else
                {
                    //Can not go here and further, stop going in this direction
                    break;
                }
            }
        }

        var bestScore = -1;
        var bestSilenceMove = (Direction.E, 0);

        foreach(var currentSilenceMove in possibleSilenceMoves)
        {
            var myPossiblePositions = MySubmarine.TrackingService.PossiblePositions;
            var trackingService = new TrackingService(myPossiblePositions);

            trackingService.Track(new SilenceAction(currentSilenceMove.Item1, currentSilenceMove.Item2));

            var score = trackingService.PossiblePositions.Count;

            if(score > bestScore)
            {
                bestScore = score;
                bestSilenceMove = currentSilenceMove;
            }
        }

        direction = bestSilenceMove.Item1;
        moves = bestSilenceMove.Item2;

        return true;
    }

    private bool TrySelectMinePosition(out Position position, out Direction direction)
    {
        position = Position.None;
        direction = Direction.E;

        if (_gameState.MineAvailable == false)
        {
            return false;
        }

        var myPosition = MySubmarine.Position;
        var neighborWaterPositions = Map.GetWaterNeighborPositions(myPosition);
        var placedMines = MySubmarine.GetPlacedMines();

        foreach (var item in neighborWaterPositions)
        {
            var possibleMinePosition = item.Item1;
            var possibleMineDirection = item.Item2;

            if (MySubmarine.HasPlacedMineAt(possibleMinePosition) == false)
            {
                var blastedPositions = GetBlastedPositions(possibleMinePosition);
                var blastOtherMines = blastedPositions.Any(p => placedMines.Contains(p));

                if (blastOtherMines == false)
                {
                    position = possibleMinePosition;
                    direction = possibleMineDirection;
                }
            }
        }

        return position.IsNot(Position.None);
    }

    private Action SelectMoveAction()
    {
        var fromPosition = MySubmarine.Position;
        var possibleMoves = GetPossibleDirectionsForMove(fromPosition);
        var possibleMoveCount = possibleMoves.Count;

        if (possibleMoveCount == 0)
        {
            return MySubmarine.SurfaceMySubmarine();
        }

        if (possibleMoveCount == 1)
        {
            var possibleMove = possibleMoves.Single();
            return MySubmarine.MoveMySubmarine(possibleMove, SelectPowerToCharge());
        }

        var visitedPositions = MySubmarine.VisitedPositions;
        var rankedMoves = new Dictionary<int, List<Tuple<Position, Direction>>>();

        foreach (var possibleMove in possibleMoves)
        {   
            var floodFillEngine = new FloodFillEngine(visitedPositions);
            var filledRegion = floodFillEngine.Run(possibleMove.Item1);
            var score = filledRegion.Count;

            if(rankedMoves.ContainsKey(score) == false)
            {
                rankedMoves[score] = new List<Tuple<Position, Direction>>();
            }

            rankedMoves[score].Add(new Tuple<Position, Direction>(possibleMove.Item1, possibleMove.Item2));
        }

        var bestMoves = rankedMoves.OrderByDescending(kvp => kvp.Key).First().Value;
        var bestMove = GetBestMoveByStealth(bestMoves);
        
        return MySubmarine.MoveMySubmarine(bestMove, SelectPowerToCharge());
    }

    private (Position,Direction) GetBestMoveByStealth(List<Tuple<Position, Direction>> moves)
    {
        if(moves.Count == 1)
            return (moves.Single().Item1, moves.Single().Item2);

        var estimationOfMyPositions = MySubmarine.TrackingService.PossiblePositions;

        var bestScore = 0;
        var bestMove = moves.First();
        var iterations = 0;

        foreach(var move in moves)
        {
            var score = ScoreMove(move, MySubmarine.VisitedPositions, estimationOfMyPositions, iterations);

            if(score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return (bestMove.Item1, bestMove.Item2);
    }

    private int ScoreMove(
        Tuple<Position, Direction> move,
        HashSet<Position> visitedPositions, 
        HashSet<Position> estimationOfMyPositions, 
        int iterations)
    {
        var trackingService = new TrackingService(estimationOfMyPositions);
        var moveAction = new MoveAction(move.Item2, Power.UNKNOWN);
        trackingService.Track(moveAction);

        if(iterations == 0)
        {
            return trackingService.PossiblePositions.Count;
        }

        // next moves ?
        var curPosition = move.Item1;
        visitedPositions.Add(curPosition);
        
        var bestScore = -1;

        foreach(var directionDelta in Player.FourDirectionDeltas)
        {
            var deltaX = directionDelta.Value.Item1;
            var deltaY = directionDelta.Value.Item2;
            var newDirection = directionDelta.Key;
            var newPos = curPosition.Translate(deltaX, deltaY);
            if(Map.IsWater(newPos) && visitedPositions.Contains(newPos) == false)
            {
                var newEstimationOfMyPositions = trackingService.PossiblePositions;
                var score = ScoreMove(
                    new Tuple<Position, Direction>(newPos, newDirection), 
                    visitedPositions.ToHashSet(),
                    trackingService.PossiblePositions.ToHashSet(),
                    iterations - 1);

                if(score > bestScore)
                {
                    bestScore = score;
                }
            }
        }

        return bestScore;
    }

    private Power SelectPowerToCharge()
    {
        if (_gameState.TorpedoAvailable == false)
        {
            return Power.TORPEDO;
        }

        if(OpponentSubmarine.Health >= 2)
        {
            if (_gameState.MineAvailable == false)
            {
                return Power.MINE;
            }
        }

        if (_gameState.SilenceAvailable == false)
        {
            return Power.SILENCE;
        }

        // if (_gameState.SonarAvailable == false)
        // {
        //     return Power.SONAR;
        // }

        return Power.SILENCE;
    }

    private List<(Position, Direction)> GetPossibleDirectionsForMove(Position myPosition)
    {
        var visitedPositions = MySubmarine.VisitedPositions;

        var possibleDirections = new List<Direction>();
        var waterNeighborPositions = Map.GetWaterNeighborPositions(myPosition)
            .Where(x => visitedPositions.Contains(x.Item1) == false);

        return waterNeighborPositions.ToList();
    }

}
