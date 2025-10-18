using System;

[Serializable]
public class EventCard : Card
{
    public EventType eventType;
    public bool mustPlayImmediately;
    public bool canSave;
}

public enum EventType
{
    ExtraRoll,
    NeedTwo,
    Challenge,
    ChineseAgent
}
