public class CardInputEvent : GameEventBase
{
    public readonly CardInputStage stage;

    public CardInputEvent(CardInputStage stage, object payload = null) 
        : base(payload)
    {
        this.stage = stage;
    }

    public override string GetName() => $"CardInputEvent({stage})";
}

public enum CardInputStage
{
    Clicked,
    Held
}