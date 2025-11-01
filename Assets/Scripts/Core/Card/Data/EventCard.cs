using System;

[Serializable]
public class EventCard : Card
{
    public override CardType CardType => CardType.Event;
    public EventType eventType;
    public EventSubType subType;
    public EventType blueTeam;
    public EventType redTeam;
    public bool mustPlayImmediately;
    public bool canSave;
    public bool canReturnToDeck;
    
    public InstitutionCard requiredInstitution;
}

public enum EventType
{
    ExtraRoll,
    NeedTwo,
    Challenge,
    LoseTurn,
    NoImpact,
    DrawnCardStays,
    TeamConditional,
    None,
    
    
}

public enum EventSubType
{
    None,
    ExtraRoll_Any,
    ExtraRoll_IfHasInstitution,
    
}
