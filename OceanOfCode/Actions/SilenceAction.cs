using System.Collections.Generic;

public class SilenceAction : Action
{
    private readonly Direction? _direction;
    private int _moves;

    public SilenceAction()
    {
        _direction = null;
    }

    /// <summary>
    /// List of direction, nb of moves and final position
    /// </summary>
    public static List<(Direction, int, Position)> ComputeSilenceActions(
        Position startingPosition, 
        HashSet<Position> alreadyVisitedPositions)
    {
        var possibleSilenceMoves = new List<(Direction, int, Position)>();

        //No move
        possibleSilenceMoves.Add((Direction.E, 0, startingPosition));

        foreach(var neighbor in Map.GetNeighborPositions(startingPosition))
        {
            //Explore this direction
            var curDirection = neighbor.Key;
            var curPos = neighbor.Value;
            var move = 1;
            while(move <= 4)
            {
                var notYetVisited = alreadyVisitedPositions.Contains(curPos) == false;
                if(notYetVisited)
                {
                    possibleSilenceMoves.Add((curDirection, move, curPos));
                    if( Map.TryGetNeighborPosition(curPos, curDirection, out var newPos))
                    {
                        curPos = newPos;
                        move++;
                    }
                    else
                    {
                        //Can not go here and further, stop going in this direction
                        break;
                    }
                }
                else
                {
                    //Can not go here and further, stop going in this direction
                    break;
                }
            }
        } 

        return possibleSilenceMoves;
    }

    public SilenceAction(Direction direction, int moves)
    {
        _direction = direction;
        _moves = moves;
    }

    public override string ToString()
    {
        return $"SILENCE {_direction.ToString()} {_moves.ToString()}";
    }
}
