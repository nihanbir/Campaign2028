using System;
using System.Collections.Generic;
using UnityEngine;

public class EventManager
{
    private readonly MainPhaseGameManager _game;
    private readonly Dictionary<EventType, Action<EventCard, Player>> _handlers;
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
    
    public void ApplyEvent(EventCard card, Player player)
    {
        Debug.Log($"Applying event {card.cardName}");
        if (_handlers.TryGetValue(card.eventType, out var handler))
        {
            handler(card, player);
            OnEventApplied?.Invoke(card);
        }
        else
            Debug.LogWarning($"Unhandled event type: {card.eventType}");
    }

    private void HandleExtraRoll(EventCard card, Player player)
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

    private void HandleNeedTwo(EventCard card, Player player)
    {
        Debug.Log($"Event 'NeedTwo' not yet implemented for {player.playerID}");
    }
    
    private void HandleLoseTurn(EventCard card, Player player)
    {
        _game.EndPlayerTurn();
    }
}