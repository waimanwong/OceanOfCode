
public class TriggerAction : Action, IWeaponAction
{
    public Position TargetPosition { get; }
    
    public TriggerAction(Position position)
    {
        TargetPosition = position;
    }

    public override string ToString()
    {
        return $"TRIGGER {TargetPosition.ToString()}";
    }
}
