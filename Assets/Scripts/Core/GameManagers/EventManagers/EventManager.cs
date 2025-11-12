using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Authoritative (local) event logic for Main Phase.
/// Refactored to broadcast through GameEventBus so UI/AI can react without tight coupling.
/// Keeps your original Unity events for backward compatibility.
/// </summary>
public class EventManager
{
    private readonly GM_MainPhase _mainPhase;
    private readonly Dictionary<EventType, IEventHandler> _handlers;

    private Player _currentPlayer;
    private EventCard _currentEventCard;
    private EventType _effectiveType;
    
    public bool IsEventActive { get; private set; }
    
    public EventManager(GM_MainPhase gm)
    {
        _mainPhase = gm;

        _handlers = new()
        {
            { EventType.ExtraRoll, new EM_ExtraRollHandler(gm, this) },
            { EventType.NeedTwo, new EM_NeedTwoHandler(gm, this) },
            { EventType.LoseTurn, new EM_LoseTurnHandler(gm, this) },
            { EventType.AlternativeStates, new EM_AltStatesHandler(gm, this) },
            { EventType.Challenge, new EM_ChallengeHandler(gm, this) },
            { EventType.NoImpact, new EM_NoImpactHandler(gm, this) },
        };
        
        GameEventBus.Instance.OnEvent += OnGameEvent;

    }
    
    public void ApplyEvent(Player player, EventCard card)
    {
        _currentPlayer = player;
        _currentEventCard = card;
        _effectiveType = ResolveEventType(card, player);

        GameEventBus.Instance.Raise(new GameEvent(EventStage.EventApplied, new EventAppliedData(card, player)));

        if (_handlers.TryGetValue(_effectiveType, out var handler))
        {
            IsEventActive = true;
            handler.Handle(player, card, _effectiveType);
        }
        else
        {
            Debug.LogWarning($"No handler for {_effectiveType}");
            EndEventImmediate(card, player);
        }
    }
    
    private void OnGameEvent(GameEvent e)
    {
        if (e.stage == EventStage.RollDiceRequest)
            OnPlayerRequestedRoll();
    }
    
    private static EventType ResolveEventType(EventCard card, Player player)
    {
        if (card.eventType != EventType.TeamBased) return card.eventType;
        return player.assignedActor.team == ActorTeam.Blue ? card.blueTeam : card.redTeam;
    }
    
    public void EndEventImmediate(EventCard card, Player player)
    {
        IsEventActive = false;
        
        GameEventBus.Instance.Raise(new GameEvent(EventStage.EventCompleted, new EventCompletedData(_effectiveType, player, card)));
        
        NullifyEventLocals();
    }
    
    public IEnumerator EndTurnAfterDelay(float seconds)
    {
        IsEventActive = false;
        
        yield return new WaitForSeconds(seconds);

        // Inform systems that this event completed
        GameEventBus.Instance.Raise(new GameEvent(EventStage.EventCompleted, new EventCompletedData(_effectiveType, _currentPlayer, _currentEventCard)));
        
        _mainPhase.EndPlayerTurn();
        
        NullifyEventLocals();
    }
    
    public void CancelEvent(EventCard card)
    {
        IsEventActive = false;
        
        Debug.Log("Event cannot be applied.");
        if (card != null && card.canReturnToDeck)
            _mainPhase.ReturnCardToDeck(card);

        //TODO: raise event canceled instead
        // Let listeners know the event ended without duel/alt flow if they care
        GameEventBus.Instance.Raise(new GameEvent(EventStage.EventCanceled, new EventCompletedData(_effectiveType, _currentPlayer, card)));

        NullifyEventLocals();
    }

    #region Helper Methods
    
    private void NullifyEventLocals()
    {
        _currentPlayer = null;
        _currentEventCard = null;
    }

    private void OnPlayerRequestedRoll()
    {
        if (!IsEventActive) return;

        GameUIManager.Instance.DiceRoll = Random.Range(1, 7);
        var roll = GameUIManager.Instance.DiceRoll;
        
        // Broadcast the roll (useful for UI dice feedback)
        GameEventBus.Instance.Raise(new GameEvent(EventStage.PlayerRolled, new PlayerRolledData(_currentPlayer, roll)));
    }
    #endregion
}


/// ======= Event Bus & Payloads (lightweight, mobile-safe) =======

public sealed class GameEventBus
{
    private static GameEventBus _instance;
    public static GameEventBus Instance => _instance ??= new GameEventBus();

    public event Action<GameEvent> OnEvent;

    public void Raise(GameEvent e)
    {
#if UNITY_EDITOR
        Debug.Log($"[EventBus] {e.stage}");
#endif
        OnEvent?.Invoke(e);
    }

    public void Clear() => OnEvent = null;
}

public readonly struct GameEvent
{
    public readonly EventStage stage;
    public readonly object Payload; // keep generic for flexibility

    public GameEvent(EventStage stage, object payload)
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

public sealed class PlayerRolledData
{
    public Player Player;
    public int    Roll;
    public PlayerRolledData(Player p, int r) { Player = p; Roll = r; }
}

public sealed class RollDiceRequest
{
    
}
