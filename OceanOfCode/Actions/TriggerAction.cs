
public class TriggerAction : Action
{
    private Position _position;

    public TriggerAction(Position position)
    {
        _position = position;
    }

    public override string ToString()
    {
        return $"TRIGGER {_position.ToString()}";
    }
}
