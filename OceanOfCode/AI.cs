using System;
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

    /// <summary>
    /// weapon target position and neighbor positions
    /// </summary>
    /// <param name="weaponPosition"></param>
    /// <returns></returns>
    private HashSet<Position> GetBlastedPositions(Position weaponPosition)
    {
        var blastedPositions = new List<Position>();
        blastedPositions.Add(weaponPosition);
        blastedPositions.AddRange(Map.NeighborBlastedPositions[weaponPosition]);
        return blastedPositions.ToHashSet();
    }

    private bool TrySilence(out Direction direction, out int moves)
    {
        direction = Direction.E;
        moves = 0;

        if(_gameState.SilenceAvailable == false || 
            MySubmarine.TrackingService.LostHealth == 0 )
        {
            return false;
        }

        var possibleSilenceMoves = SilenceAction.ComputeSilenceActions(MySubmarine.Position, MySubmarine.VisitedPositions);

        var bestScore = -1;
        var bestSilenceMove = (Direction.E, 0, MySubmarine.Position);

        Player.Debug($"{possibleSilenceMoves.Count} possibleSilenceMoves");

        foreach(var currentSilenceMove in possibleSilenceMoves)
        {
            Player.Debug($"Evaluate {currentSilenceMove}");

            var myPossiblePositions = MySubmarine.TrackingService.PossiblePositions;
            var lastMoveAction = MySubmarine.TrackingService.LastMoveAction;
            var trackingService = new TrackingService(myPossiblePositions, lastMoveAction);

            trackingService.Track(new SilenceAction(currentSilenceMove.Item1, currentSilenceMove.Item2));

            var score = trackingService.PossiblePositions.Count;

            if(score > bestScore)
            {
                bestScore = score;
                bestSilenceMove = currentSilenceMove;
             
                Player.Debug($"bestSilenceMove: {bestSilenceMove} ({score})");
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
        var neighborWaterPositions = Map.GetNeighborPositions(myPosition);
        var placedMines = MySubmarine.GetPlacedMines();

        foreach (var item in neighborWaterPositions)
        {
            var possibleMinePosition = item.Value;
            var possibleMineDirection = item.Key;

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
        var lastMoveAction = MySubmarine.TrackingService.LastMoveAction;

        var bestScore = 0;
        var bestMove = moves.First();
        var iterations = 0;

        foreach(var move in moves)
        {
            var score = ScoreMove(move, MySubmarine.VisitedPositions, estimationOfMyPositions, lastMoveAction, iterations);

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
        MoveAction lastMoveAction,
        int iterations)
    {
        var trackingService = new TrackingService(estimationOfMyPositions, lastMoveAction);
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
        var neighborPositions = Map.GetNeighborPositions(curPosition);

        foreach(var neighbor in neighborPositions)
        {
            var newDirection = neighbor.Key;
            var newPos = neighbor.Value;
        
            if(visitedPositions.Contains(newPos) == false)
            {
                var newEstimationOfMyPositions = trackingService.PossiblePositions;
                var score = ScoreMove(
                    new Tuple<Position, Direction>(newPos, newDirection), 
                    visitedPositions.ToHashSet(),
                    trackingService.PossiblePositions.ToHashSet(),
                    lastMoveAction,
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

        if (_gameState.SilenceAvailable == false)
        {
            return Power.SILENCE;
        }

        if(OpponentSubmarine.Health >= 2)
        {
            if (_gameState.MineAvailable == false)
            {
                return Power.MINE;
            }
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

        return Map.GetNeighborPositions(myPosition)
            .Where(x => visitedPositions.Contains(x.Value) == false)
            .Select( kvp => (kvp.Value, kvp.Key))
            .ToList();
    }

}
