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

        if(selectedActions.OfType<SilenceAction>().Any() == false)
        {
            var selectedMoveAction = SelectMoveAction();
            actions.Add(selectedMoveAction);
        }

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

        return torpedoPosition != Position.None;
    }

    private bool TryTriggerMine(out Position bestMinePosition)
    { 
        bestMinePosition = Position.None;

        var enemyPositions = OpponentSubmarine.PossiblePositions;

        if (enemyPositions.Count > 10)
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
                var count = enemyPositions.Count(p => blastedPositions.Contains(p));

                if (count > blastedOpponentPositions)
                {
                    blastedOpponentPositions = count;
                    bestMinePosition = minePosition;
                }
            }
        }

        return bestMinePosition != Position.None;
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

        if (_gameState.SilenceAvailable == false)
        {
            return false;
        }

        var myPosition = MySubmarine.Position;

        var possibleSilenceMoves = new HashSet<(Direction, int)>();

        foreach (var kvp in Player.FourDirectionDeltas)
        {
            var d = kvp.Key;
            var delta = kvp.Value;

            var currentPosition = myPosition;
            for (int step = 1; step <= 4; step++)
            {
                currentPosition = currentPosition.Translate(delta.Item1, delta.Item2);
                if (Map.IsWater(currentPosition) && MySubmarine.VisitedPositions.Contains(currentPosition) == false)
                {
                    //ok
                    possibleSilenceMoves.Add((d, step));
                }
                else
                {
                    break;
                }
            }
        }

        if (possibleSilenceMoves.Count == 0)
        {
            return false;
        }

        var random = new Random((int)Stopwatch.GetTimestamp());
        var selectedIndex = random.Next(0, possibleSilenceMoves.Count - 1);

        var selectedMove = possibleSilenceMoves.ElementAt(selectedIndex);
        direction = selectedMove.Item1;
        moves = selectedMove.Item2;
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
        var neighborWaterPositions = Map.GetNeighborPositions(myPosition)
            .Where(x => Map.IsWater(x.Item1))
            .ToList();
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

        return position != Position.None;
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

        var bestMove = possibleMoves.First();
        var bestScore = 0;
        var bestFilledRegion = new HashSet<Position>();
        var visitedPositions = MySubmarine.VisitedPositions;

        foreach (var possibleMove in possibleMoves)
        {
            if (bestFilledRegion.Contains(possibleMove.Item1))
            {
                //the possible move result in the same bestfilled region
                var freedomScore = new Func<Position, int>(pos =>
                       Map.GetNeighborPositions(pos)
                           .Count(p => Map.IsWater(p.Item1) && visitedPositions.Contains(p.Item1) == false));

                var bestPositionFreedomScore = freedomScore(bestMove.Item1);
                var currentMoveFreedomScore = freedomScore(possibleMove.Item1);

                if (currentMoveFreedomScore < bestPositionFreedomScore)
                {
                    //go toward position with least freedom
                    bestMove = possibleMove;
                }
            }
            else
            {
                var floodFillEngine = new FloodFillEngine(visitedPositions);

                var filledRegion = floodFillEngine.Run(possibleMove.Item1);

                var score = filledRegion.Count;

                if (score > bestScore)
                {
                    //Go to the position with largest region
                    bestScore = score;
                    bestMove = possibleMove;
                    bestFilledRegion = filledRegion;
                }
            }
        }

        return MySubmarine.MoveMySubmarine(bestMove, SelectPowerToCharge());
    }

    private Power SelectPowerToCharge()
    {
        if (_gameState.TorpedoAvailable == false)
        {
            return Power.TORPEDO;
        }

        if (MySubmarine.TrackingService.PossiblePositions.Count > 30)
        {
            if (_gameState.MineAvailable == false)
            {
                return Power.MINE;
            }

            if (_gameState.SilenceAvailable == false)
            {
                return Power.SILENCE;
            }

            if (_gameState.SonarAvailable == false)
            {
                return Power.SONAR;
            }

        }
        else
        {
            if (_gameState.SilenceAvailable == false)
            {
                return Power.SILENCE;
            }
            if (_gameState.MineAvailable == false)
            {
                return Power.MINE;
            }
        }
        return Power.MINE;
    }

    private List<(Position, Direction)> GetPossibleDirectionsForMove(Position myPosition)
    {
        var visitedPositions = MySubmarine.VisitedPositions;

        var possibleDirections = new List<Direction>();
        var waterNeighborPositions = Map.GetNeighborPositions(myPosition)
            .Where(x => Map.IsWater(x.Item1))
            .Where(x => visitedPositions.Contains(x.Item1) == false);

        return waterNeighborPositions.ToList();
    }

}
