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
    private readonly GameManager _gm;
    private readonly GM_MainPhase _mainPhase;
    private readonly Dictionary<EventType, IEventHandler> _handlers;

    private Player _currentPlayer;
    private EventCard _currentEventCard;
    private EventType _effectiveType;

    private bool _isEventActive = false;
    public bool IsEventScreen { get; set; }
    
    public EventManager(GM_MainPhase gm)
    {
        _gm = GameManager.Instance;
        
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
    }
    
    private void OnEventCardEvent(EventCardEvent e)
    {
        if (e.stage == EventStage.RollDiceRequest)
            OnPlayerRequestedRoll();
        
        if (e.stage == EventStage.PlayerRolled)
        {
            if (!_isEventActive) return;

            var data = (PlayerRolledData)e.payload;

            // Forward roll to the active handler
            if (_handlers.TryGetValue(_effectiveType, out var handler))
                handler.EvaluateRoll(data.Player, data.Roll);
        }
    }

    public void ApplyEvent(Player player, EventCard card)
    {
        _currentPlayer = player;
        _currentEventCard = card;
        _effectiveType = ResolveEventType(card, player);
        
        TurnFlowBus.Instance.Raise(new EventCardEvent(EventStage.EventApplied));
        
        EventCardBus.Instance.Raise(new EventCardEvent(EventStage.EventApplied, new EventAppliedData(card, player)));
        
        if (_handlers.TryGetValue(_effectiveType, out var handler))
        {
            EventCardBus.Instance.OnEvent += OnEventCardEvent;
            
            _isEventActive = true;
            handler.Handle(player, card, _effectiveType);
        }
        else
        {
            Debug.LogWarning($"No handler for {_effectiveType}");
            CompleteEvent();
        }
    }
    
    private static EventType ResolveEventType(EventCard card, Player player)
    {
        if (card.eventType != EventType.TeamBased) return card.eventType;
        return player.assignedActor.team == ActorTeam.Blue ? card.blueTeam : card.redTeam;
    }

    public void CompleteEvent()
    {
        EventCardBus.Instance.OnEvent -= OnEventCardEvent;
        
        _isEventActive = false;
        
        EventCardBus.Instance.Raise(new EventCardEvent(EventStage.EventCompleted, new EventCompletedData(_effectiveType, _currentPlayer, _currentEventCard)));
        
        NullifyEventLocals();
    }

    public void CompleteDuel()
    {
        IsEventScreen = false;
        
        EventCardBus.Instance.Raise(new EventCardEvent(EventStage.DuelCompleted));
        
        TurnFlowBus.Instance.Raise(new EventCardEvent(EventStage.DuelCompleted));
        
        CompleteEvent();
        
        _mainPhase.EndPlayerTurnFromEvent();
    }
    
    public void CancelEvent(EventCard card)
    {
        EventCardBus.Instance.OnEvent -= OnEventCardEvent;
        
        _isEventActive = false;
        
        Debug.Log("Event cannot be applied.");
        if (card != null && card.canReturnToDeck)
            _mainPhase.ReturnCardToDeck(card);
        
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
        if (!_isEventActive) return;
        
        var roll = Random.Range(1, 7);
        
        EventCardBus.Instance.Raise(new EventCardEvent(EventStage.PlayerRolled, new PlayerRolledData(_currentPlayer, roll)));
    }
    #endregion
}


