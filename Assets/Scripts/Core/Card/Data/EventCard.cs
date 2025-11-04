using System;

[Serializable]
public class EventCard : Card
{
    public override CardType CardType => CardType.Event;
    public EventType eventType;
    public EventConditions eventConditions;
    public EventType blueTeam;
    public EventType redTeam;
    public ActorTeam benefitingTeam;
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
    TeamBased,
    None,
    
}

public enum EventConditions
{
    None,
    Any,
    IfOwnsInstitution,
    IfInstitutionCaptured,
    TeamConditions,
}
