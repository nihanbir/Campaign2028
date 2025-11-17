public abstract class GameEventBase : IGameEvent
{
    public readonly object payload;

    protected GameEventBase(object payload)
    {
        this.payload = payload;
    }

    public abstract string GetName();

    public override string ToString()
        => $"{GetName()} Payload={payload}";
}