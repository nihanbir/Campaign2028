using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager
{
    private readonly MainPhaseGameManager _game;
    private readonly Dictionary<EventType, Action<Player, EventCard>> _handlers;
    public event Action<EventCard> OnEventApplied;

    public EventManager(MainPhaseGameManager gm)
    {
        _game = gm;
        _handlers = new()
        {
            { EventType.ExtraRoll, HandleExtraRoll },
            { EventType.NeedTwo, HandleNeedTwo },
            { EventType.LoseTurn, HandleLoseTurn }
        };
        
    }
    
    public void ApplyEvent(Player player, EventCard card)
    {
        Debug.Log($"Applying event {card.cardName}");
        if (_handlers.TryGetValue(card.eventType, out var handler))
        {
            handler(player, card);
            OnEventApplied?.Invoke(card);
        }
        else
            Debug.LogWarning($"Unhandled event type: {card.eventType}");
    }

    private void HandleExtraRoll(Player player, EventCard card)
    {
        bool canApply = card.subType switch
        {
            EventSubType.ExtraRoll_IfHasInstitution => player.HasInstitution(card.requiredInstitution),
            EventSubType.ExtraRoll_Any => true,
            _ => false
        };

        if (canApply)
            player.AddExtraRoll();
        else
            _game.ReturnCardToDeck(card);
    }

    private void HandleNeedTwo(Player player, EventCard card)
    {
        Debug.Log($"Event 'NeedTwo' not yet implemented for {player.playerID}");
    }
    
    private void HandleLoseTurn(Player player, EventCard card)
    {
        _game.EndPlayerTurn();
    }
}