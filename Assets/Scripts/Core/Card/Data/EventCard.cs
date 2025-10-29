using System;

[Serializable]
public class EventCard : Card
{
    public override CardType CardType => CardType.Event;
    public EventType eventType;
    public bool mustPlayImmediately;
    public bool canSave;
}

public enum EventType
{
    ExtraRoll,
    NeedTwo,
    Challenge,
    LoseTurn,
    ChineseAgent
}
