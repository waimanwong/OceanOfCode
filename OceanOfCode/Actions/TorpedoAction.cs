
public class TorpedoAction : Action
{
    public static int Range = 4;
    public readonly Position TargetPosition;

    public TorpedoAction(Position position)
    {
        TargetPosition = position;
    }

    public override string ToString()
    {
        return $"TORPEDO {TargetPosition.ToString()}";
    }
}
