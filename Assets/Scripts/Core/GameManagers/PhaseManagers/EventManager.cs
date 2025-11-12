using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Authoritative (local) event logic for Main Phase.
/// Refactored to broadcast through GameEventBus so UI/AI can react without tight coupling.
/// Keeps your original Unity events for backward compatibility.
/// </summary>
public class EventManager
{
    private readonly GM_MainPhase _mainPhase;

    // Event runtime flags/state
    private bool _needTwoActive = false;
    private bool _eventActive = false;
    public bool IsEventActive => _eventActive;

    private EventType _effectiveType;
    private EventCard _currentEventCard;
    private Player _currentPlayer;

    // Alt state vars
    private StateCard _altState1;
    private StateCard _altState2;

    // Duel vars
    private Card _chosenCard;
    private Player _defender;

    private readonly Dictionary<EventType, Action<Player, EventCard>> _handlers;

    public EventManager(GM_MainPhase gm)
    {
        _mainPhase = gm;

        _handlers = new()
        {
            { EventType.ExtraRoll, HandleExtraRoll },
            { EventType.NeedTwo, HandleNeedTwo },
            { EventType.LoseTurn, HandleLoseTurn },
            { EventType.AlternativeStates, HandleAlternativeStates },
            { EventType.Challenge, HandleChallenge },
            { EventType.NoImpact, (p, c) => {} },
            { EventType.TeamBased, (p, c) => HandleTeamBased(p, c) }
        };
        
        GameEventBus.Instance.OnEvent += OnGameEvent;

    }
    
    public void ApplyEvent(Player player, EventCard card)
    {
        _currentPlayer = player;
        _currentEventCard = card;

        // Resolve effective type (including team-based)
        _effectiveType = ResolveEventType(card, player);
        
        Debug.Log($"EventManager ApplyEvent: {card.cardName} resolved type={_effectiveType}, handlers count={_handlers.Count}");
        
        // Legacy callback + bus fire (applied does NOT mean resolved)
        GameEventBus.Instance.Raise(new GameEvent(EventStage.EventApplied, new EventAppliedData(card, player)));
        
        if (_handlers.TryGetValue(_effectiveType, out var handler))
        {
            handler(player, card);
        }
        else
        {
            Debug.LogWarning($"Unhandled event type: {_effectiveType}");
        }
    }

    private static EventType ResolveEventType(EventCard card, Player player)
    {
        if (card.eventType != EventType.TeamBased) return card.eventType;
        return player.assignedActor.team == ActorTeam.Blue ? card.blueTeam : card.redTeam;
    }

    #region Extra Roll
    private void HandleExtraRoll(Player player, EventCard card)
    {
        bool canApply = card.eventConditions switch
        {
            EventConditions.IfOwnsInstitution => player.HasInstitution(card.requiredInstitution),
            EventConditions.None => true,
            _ => false
        };

        if (canApply)
        {
            player.AddExtraRoll();
        }
        else if (card.canReturnToDeck)
        {
            _mainPhase.ReturnCardToDeck(card);
        }

        // No blocking UI; event is resolved immediately
        EndEventIfIdle();
    }
    #endregion

    #region Need Two
    private void HandleNeedTwo(Player player, EventCard card)
    {
        _needTwoActive = true;
        EndEventIfIdle();
    }

    public bool ConsumeNeedTwo()
    {
        if (!_needTwoActive) return false;
        _needTwoActive = false;
        return true;
    }
    #endregion

    #region Lose Turn
    private void HandleLoseTurn(Player player, EventCard card)
    {
        _eventActive = true;

        // Broadcast start so UI can show small feedback if desired
        GameEventBus.Instance.Raise(new GameEvent(EventStage.EventStarted, new EventStartedData(_effectiveType, player, card)));

        //TODO: instead of this end player turn without moving to next player
        // Small delay to let any UI coroutines/animations breathe; then end turn
        GameManager.Instance.StartCoroutine(EndTurnAfterDelay(2f));
    }

    private IEnumerator EndTurnAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        _eventActive = false;

        // Inform systems that this event completed
        GameEventBus.Instance.Raise(new GameEvent(EventStage.EventCompleted, new EventCompletedData(_effectiveType, _currentPlayer, _currentEventCard)));
        
        _mainPhase.EndPlayerTurn();
        
        NullifyEventLocals();
    }
    #endregion

    #region Alternative States
    private void HandleAlternativeStates(Player player, EventCard card)
    {
        // NOTE: your original code looked up altState2 using altState1 by mistake.
        // Fixed to use card.altState2 for second find.
        _altState1 = _mainPhase.FindStateFromDeck(card.altState1, out var found1);
        _altState2 = _mainPhase.FindStateFromDeck(card.altState2, out var found2);

        // If neither found, cancel
        if (!found1 && !found2)
        {
            CancelEvent(card);
            return;
        }

        _eventActive = true;
        _currentPlayer = player;

        // Legacy UI event
        // OnAltStatesActive?.Invoke(player, _altState1, _altState2);

        // Bus event for decoupled UI
        GameEventBus.Instance.Raise(new GameEvent(
            EventStage.AltStatesShown,
            new AltStatesData(player, _altState1, _altState2, card)));

        // Let UI/AI handle the dice roll; we stay idle until a roll arrives via OnPlayerRolledDice()
    }

    public void EvaluateStateDiscard(int roll)
    {
        StateCard cardToDiscard = null;
        switch (roll)
        {
            case 1: if (_altState1 != null) cardToDiscard = _altState1; break;
            case 2: if (_altState2 != null) cardToDiscard = _altState2; break;
        }

        if (cardToDiscard != null)
        {
            _mainPhase.DiscardState(cardToDiscard);
        }
        else
        {
            Debug.Log($"Player {_currentPlayer.playerID} didn't discard any states!");
        }
        
        GameManager.Instance.StartCoroutine(EndTurnAfterDelay(2f));
    }
    #endregion

    #region Challenge
    private void HandleChallenge(Player player, EventCard card)
    {
        switch (card.eventConditions)
        {
            case EventConditions.Any:
                ChallengeAnyState(player, card);
                break;
                
            case EventConditions.IfInstitutionCaptured:
                ChallengeInstitution(player, card);
                break;

            default:
                CancelEvent(card);
                break;
        }
    }
    
    public void EvaluateChallengeCapture(int roll)
    {
        bool success = _chosenCard switch
        {
            StateCard s       => s.IsSuccessfulRoll(roll, _currentPlayer.assignedActor.team),
            InstitutionCard i => i.IsSuccessfulRoll(roll, _currentPlayer.assignedActor.team),
            _                 => false
        };

        Debug.Log($"rolled: {roll}");
           
        if (success)
        {
            _mainPhase.UpdateCardOwnership(_currentPlayer, _chosenCard);
        }
        else
        {
            _mainPhase.ReturnCardToDeck(_currentEventCard);
            Debug.Log($"Player {_currentPlayer.playerID} failed to capture {_chosenCard.cardName}");
        }
        
        GameManager.Instance.StartCoroutine(EndTurnAfterDelay(2.5f));
    }
    #endregion

    #region Challenge Institution
    private void ChallengeInstitution(Player player, EventCard card)
    {
        _currentPlayer = player;

        _chosenCard = _mainPhase.FindHeldInstitution(card.requiredInstitution, out var cardFound);
        
        if (!cardFound)
        {
            CancelEvent(card);
            return;
        }

        var cardHolder = _mainPhase.GetCardHolder(_chosenCard);
        if (cardHolder && cardHolder != player)
        {
            _defender = cardHolder;
        }
        
        if (!_defender)
        {
            CancelEvent(card);
            return;
        }
        
        _eventActive = true;

        // Legacy + bus
        GameEventBus.Instance.Raise(new GameEvent(EventStage.DuelStarted, new DuelData(player, _defender, _chosenCard, _currentEventCard)));
        
    }
    #endregion

    #region Challenge Any State
    private void ChallengeAnyState(Player player, EventCard card)
    {
        var availableStates = GetChallengableStatesForPlayer(player);
        if (availableStates == null)
        {
            CancelEvent(card);
            return;
        }
            
        _currentPlayer = player;
        _eventActive   = true;

        // Legacy + bus
        
        GameEventBus.Instance.Raise(new GameEvent(EventStage.ChallengeStateShown, new ChallengeStatesData(player, availableStates, card)));
    }

    private List<StateCard> GetChallengableStatesForPlayer(Player player)
    {
        var stateOwners     = _mainPhase.GetStateOwners();
        var availableStates = new List<StateCard>();

        foreach (var kvp in stateOwners)
        {
            if (kvp.Value == player) continue;
            availableStates.Add(kvp.Key);
        }

        return availableStates.Count == 0 ? null : availableStates;
    }

    // Public for AI/UI
    public void HandleStateChosen(StateCard chosenState)
    {
        _chosenCard = chosenState;

        _defender = _mainPhase.GetCardHolder(_chosenCard);

        // Defender could be null if something desynced; cancel safely
        if (!_defender)
        {
            CancelEvent(_currentEventCard);
            return;
        }

        // Legacy + bus
        
        GameEventBus.Instance.Raise(new GameEvent(EventStage.DuelStarted, new DuelData(_currentPlayer, _defender, _chosenCard, _currentEventCard)));
    }
    #endregion

    #region Helper Methods
    private void CancelEvent(EventCard card)
    {
        Debug.Log("Event cannot be applied.");
        if (card != null && card.canReturnToDeck)
            _mainPhase.ReturnCardToDeck(card);

        //TODO: raise event canceled instead
        // Let listeners know the event ended without duel/alt flow if they care
        GameEventBus.Instance.Raise(new GameEvent(EventStage.EventCompleted, new EventCompletedData(_effectiveType, _currentPlayer, card)));

        NullifyEventLocals();
        _eventActive = false;
    }

    private void NullifyEventLocals()
    {
        _chosenCard   = null;
        _defender     = null;
        _altState1    = null;
        _altState2    = null;
        _currentPlayer = null;
        _currentEventCard = null;
    }

    private void EndEventIfIdle()
    {
        // For instant-resolve events (ExtraRoll / NeedTwo), let systems know we’re done.
        GameEventBus.Instance.Raise(new GameEvent(EventStage.EventCompleted, new EventCompletedData(_effectiveType, _currentPlayer, _currentEventCard)));
        NullifyEventLocals();
        _eventActive = false;
    }

    private void OnPlayerRolledDice(int roll)
    {
        if (!_eventActive) return;

        // Broadcast the roll (useful for UI dice feedback)
        GameEventBus.Instance.Raise(new GameEvent(EventStage.PlayerRolled, new PlayerRolledData(_currentPlayer, roll)));

        if (_effectiveType == EventType.AlternativeStates)
        {
            EvaluateStateDiscard(roll);
        }
        else
        {
            EvaluateChallengeCapture(roll);
        }
    }

    private void HandleTeamBased(Player p, EventCard c)
    {
        // TeamBased resolves to a concrete type earlier; this is just a guard.
        Debug.LogWarning("TeamBased handler should not execute directly.");
    }
    #endregion
    
    private void OnGameEvent(GameEvent e)
    {
        if (e.stage != EventStage.RollDiceRequest)
            return;

        var data = (PlayerRolledData)e.Payload;
        // 🔹 This is the exact same logic as before, but now triggered by bus
        if (_currentPlayer == data.Player)
        {
            OnPlayerRolledDice(data.Roll);
        }
    }

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
