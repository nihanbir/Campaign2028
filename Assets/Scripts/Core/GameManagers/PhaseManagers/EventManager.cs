using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager
{
    private readonly MainPhaseGameManager _game;
    private readonly Dictionary<EventType, Action<Player, EventCard>> _handlers;
    public event Action<EventCard> OnEventApplied;

    private bool _needTwoActive = false;

    public EventManager(MainPhaseGameManager gm)
    {
        _game = gm;
        _handlers = new()
        {
            { EventType.ExtraRoll, HandleExtraRoll },
            { EventType.NeedTwo, HandleNeedTwo },
            { EventType.LoseTurn, HandleLoseTurn },
            { EventType.NoImpact, (p, c) => {} }
        };
        
    }
    
    public void ApplyEvent(Player player, EventCard card)
    {
        Debug.Log($"Applying event {card.cardName}");
        EventType effectiveType = card.eventType;

        if (card.eventType == EventType.TeamConditional)
        {
            effectiveType = player.assignedActor.team == ActorTeam.Blue ? card.blueTeam : card.redTeam;
            Debug.Log($"-------------------------------------------------------------{effectiveType}");
        }

        if (_handlers.TryGetValue(effectiveType, out var handler))
        {
            handler(player, card);
            OnEventApplied?.Invoke(card);
        }
        else
            Debug.LogWarning($"Unhandled event type: {effectiveType}");
    }

    private void HandleExtraRoll(Player player, EventCard card)
    {
        bool canApply = card.subType switch
        {
            EventSubType.ExtraRoll_IfHasInstitution => player.HasInstitution(card.requiredInstitution),
            EventSubType.ExtraRoll_Any => true,
            EventSubType.None => true, // ✅ unconditional (e.g., resolved from TeamConditional)
            _ => false
        };

        if (canApply)
            player.AddExtraRoll();
        else if (card.canReturnToDeck)
            _game.ReturnCardToDeck(card);
    }

    private void HandleNeedTwo(Player player, EventCard card)
    {
        _needTwoActive = true;
       }
    
    private void HandleLoseTurn(Player player, EventCard card)
    {
        _game.EndPlayerTurn();
    }
    
    public bool ConsumeNeedTwo()
    {
        if (!_needTwoActive)
            return false;

        _needTwoActive = false;
        return true;
    }
}