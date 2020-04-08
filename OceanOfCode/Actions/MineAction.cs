
public class MineAction : Action
{
    private readonly Direction _direction;

    public MineAction(Direction direction)
    {
        _direction = direction;
    }

    public override string ToString()
    {
        return $"MINE {_direction.ToString()}";
    }
}
