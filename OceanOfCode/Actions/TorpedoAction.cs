
public class TorpedoAction : Action, IWeaponAction
{
    public Position TargetPosition { get; }

    public TorpedoAction(Position position)
    {
        TargetPosition = position;
    }

    public override string ToString()
    {
        return $"TORPEDO {TargetPosition.ToString()}";
    }
}
