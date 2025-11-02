using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager
{
    private readonly MainPhaseGameManager _game;
    private readonly Dictionary<EventType, Action<Player, EventCard>> _handlers;
    public event Action<EventCard> OnEventApplied;
    public event Action<List<StateCard>> OnChallengeState;

    private bool _needTwoActive = false;

    public EventManager(MainPhaseGameManager gm)
    {
        _game = gm;
        _handlers = new()
        {
            { EventType.ExtraRoll, HandleExtraRoll },
            { EventType.NeedTwo, HandleNeedTwo },
            { EventType.LoseTurn, HandleLoseTurn },
            { EventType.Challenge, HandleChallenge },
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
#region Extra Roll

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
    
#endregion Extra Roll

#region Need Two
    private void HandleNeedTwo(Player player, EventCard card)
    {
        _needTwoActive = true;
    }
    public bool ConsumeNeedTwo()
    {
        if (!_needTwoActive)
            return false;

        _needTwoActive = false;
        return true;
    }
#endregion Need Two

#region Lose Turn

    private void HandleLoseTurn(Player player, EventCard card)
    {
        _game.EndPlayerTurn();
    }
    
#endregion Lose Turn

#region Challenge

    private void HandleChallenge(Player player, EventCard card)
    {
        bool challengeAnyState = card.subType switch
        {
            EventSubType.Challenge_AnyState => true,
            _ => false
        };

        if (challengeAnyState)
        {
            var heldStates = _game.GetHeldStates();

            if (heldStates.Count == 0)
            {
                Debug.Log("No states are currently held. Challenge cannot be applied.");
                if (card.canReturnToDeck)
                    _game.ReturnCardToDeck(card);
                return;
            }

            List<StateCard> statesToDisplay = new();
            statesToDisplay.AddRange(_game.GetHeldStates().Values);
            
            
            OnChallengeState?.Invoke(statesToDisplay);
            
        }
    }

#endregion Challenge



}