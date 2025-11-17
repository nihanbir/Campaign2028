using System.Collections.Generic;

public class MainStageEvent : GameEventBase
{
    public readonly MainStage stage;
    
    public MainStageEvent(MainStage stage, object payload = null) 
        : base(payload)
    {
        this.stage = stage;
    }

    public override string GetName() => $"MainStageEvent({stage})";
}

public enum MainStage
{
    StateDiscarded,
    CardCaptured,
    CardLost,
    CardReturnedToDeck,
    
    DrawEventCardRequest,
    EventCardDrawn,
    SaveEventCardRequest,
    EventCardSaved,
    ApplyEventCardRequest,
    CurrentEventCardCleared,
    
    DrawTargetCardRequest,
    TargetCardDrawn,
    CurrentTargetCardCleared,
    
}

public sealed class CardOwnerChangedData
{
    public Player owner;
    public Player newOwner;
    public Card card;
    
    public CardOwnerChangedData(Player p, Player newp, Card c) { owner = p; newOwner = newp; card = c; }
}

public sealed class CardCapturedData
{
    public Player player;
    public Card card;
    
    public CardCapturedData(Player p, Card c) { player = p; card = c; }
}

public sealed class EventCardClearedData
{
    public EventCard card;

    public EventCardClearedData(EventCard e) { card = e; }
}