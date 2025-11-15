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
        
        EventCardBus.Instance.OnEvent += OnGameEvent;

    }
    
    public void ApplyEvent(Player player, EventCard card)
    {
        _currentPlayer = player;
        _currentEventCard = card;
        _effectiveType = ResolveEventType(card, player);

        EventCardBus.Instance.Raise(new EventCardEvent(EventStage.EventApplied, new EventAppliedData(card, player)));

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
    
    private void OnGameEvent(EventCardEvent e)
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
        
        EventCardBus.Instance.Raise(new EventCardEvent(EventStage.EventCompleted, new EventCompletedData(_effectiveType, player, card)));
        
        NullifyEventLocals();
    }
    
    public IEnumerator EndTurnAfterDelay(float seconds)
    {
        IsEventActive = false;
        
        yield return new WaitForSeconds(seconds);

        // Inform systems that this event completed
        EventCardBus.Instance.Raise(new EventCardEvent(EventStage.EventCompleted, new EventCompletedData(_effectiveType, _currentPlayer, _currentEventCard)));
        
        //TODO: can have its own endplayerturn logic
        // _mainPhase.EndPlayerTurn();
        
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
        EventCardBus.Instance.Raise(new EventCardEvent(EventStage.EventCanceled, new EventCompletedData(_effectiveType, _currentPlayer, card)));

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
        
        //TODO: should we?
        // Broadcast the roll (useful for UI dice feedback)
        // EventCardBus.Instance.Raise(new CardEvent(EventStage.PlayerRolled, new PlayerRolledData(_currentPlayer, roll)));
    }
    #endregion
}


