public class MessageAction : Action
{
    private readonly string _message;

    public MessageAction(string  message)
    {
        _message = message;
    }

    public override string ToString()
    {
        return $"MSG {_message}";
    }
}