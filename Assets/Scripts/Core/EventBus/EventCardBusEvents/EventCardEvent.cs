using System.Collections.Generic;

public readonly struct EventCardEvent
{
    public readonly EventStage stage;
    public readonly object Payload; // keep generic for flexibility

    public EventCardEvent(EventStage stage, object payload)
    {
        this.stage = stage;
        Payload = payload;
    }
}

public enum EventStage
{
    None,
    EventApplied,          // Sent when ApplyEvent() is called (not resolved)
    EventStarted,          // When a blocking event begins (LoseTurn, Duel, AltStates)
    EventCompleted,        // After an event fully resolves
    EventCanceled,
    ChallengeStateShown,   // Show list of states to choose
    DuelStarted,           // Attacker vs Defender with chosen card
    DuelCompleted,         // Duel done
    AltStatesShown,        // Alt states UI should appear
    PlayerRolled,           // Player rolled value (for UI dice feedback)
    ClientAnimationCompleted,
    RollDiceRequest,

}

// Strongly-typed payloads (class for clarity, can be structs if you want)
public sealed class DuelData
{
    public Player Attacker;
    public Player Defender;
    public Card   ChosenCard;
    public EventCard SourceEvent;
    public DuelData(Player a, Player d, Card c, EventCard src) { Attacker = a; Defender = d; ChosenCard = c; SourceEvent = src; }
}

public sealed class AltStatesData
{
    public Player Player;
    public StateCard State1;
    public StateCard State2;
    public EventCard SourceEvent;
    public AltStatesData(Player p, StateCard s1, StateCard s2, EventCard src) { Player = p; State1 = s1; State2 = s2; SourceEvent = src; }
}

public sealed class ChallengeStatesData
{
    public Player Player;
    public List<StateCard> States;
    public EventCard SourceEvent;
    public ChallengeStatesData(Player p, List<StateCard> list, EventCard src) { Player = p; States = list; SourceEvent = src; }
}

public sealed class EventAppliedData
{
    public EventCard Card;
    public Player    Player;
    public EventAppliedData(EventCard c, Player p) { Card = c; Player = p; }
}

public sealed class EventStartedData
{
    public EventType Type;
    public Player    Player;
    public EventCard Card;
    public EventStartedData(EventType t, Player p, EventCard c) { Type = t; Player = p; Card = c; }
}

public sealed class EventCompletedData
{
    public EventType Type;
    public Player    Player;
    public EventCard Card;
    public EventCompletedData(EventType t, Player p, EventCard c) { Type = t; Player = p; Card = c; }
}