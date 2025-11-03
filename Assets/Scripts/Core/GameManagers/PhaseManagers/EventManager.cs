using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EventManager
{
    private readonly MainPhaseGameManager _mainPhase;
    private readonly Dictionary<EventType, Action<Player, EventCard>> _handlers;
    public event Action<EventCard> OnEventApplied;

    private bool _needTwoActive = false;
    private EventCard _currentEventCard;

    public EventManager(MainPhaseGameManager gm)
    {
        _mainPhase = gm;
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
            _currentEventCard = card;
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
            _mainPhase.ReturnCardToDeck(card);
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
        _mainPhase.EndPlayerTurn();
    }
    
#endregion Lose Turn

#region Challenge

    public event Action<List<StateCard>> OnChallengeState;
    public event Action<Player, StateCard> OnDuelActive;
    public event Action OnDuelCompleted;
    
    private StateCard _chosenState;
    private Player _defender;
    private Player _attacker;

    private void HandleChallenge(Player player, EventCard card)
    {
        bool challengeAnyState = card.subType switch
        {
            EventSubType.Challenge_AnyState => true,
            _ => false
        };

        if (challengeAnyState)
        {
            var heldStates = _mainPhase.GetHeldStates();

            if (heldStates.Count == 0)
            {
                Debug.Log("No states are currently held. Challenge cannot be applied.");
                if (card.canReturnToDeck)
                    _mainPhase.ReturnCardToDeck(card);
                return;
            }

            List<StateCard> availableStates = new();
            availableStates.AddRange(_mainPhase.GetHeldStates().Values);
            
            // //TODO: don't forget to remove this
            // GameManager.Instance.currentPlayerIndex = GameManager.Instance.players.FindIndex(p => p.playerID == 0);
            
            _attacker = GameManager.Instance.CurrentPlayer;
            
            OnChallengeState?.Invoke(availableStates);
            
            if (AIManager.Instance.IsAIPlayer(player))
            {
                var aiPlayer = AIManager.Instance.GetAIPlayer(player);
                GameManager.Instance.StartCoroutine(AIManager.Instance.mainAI.HandleChooseState(aiPlayer, availableStates));
            }
            
            StateDisplayCard.OnCardHeld += HandleStateChosen;
            
        }
    }

    //Set to public for AI
    public void HandleStateChosen(StateCard chosenState)
    {
        StateDisplayCard.OnCardHeld -= HandleStateChosen;
        
        _chosenState = chosenState;
        
        Debug.Log($"Player held {_chosenState.cardName} → set as challenge target.");
    
        _defender = _mainPhase.GetHeldStates().FirstOrDefault(player => player.Value == _chosenState).Key;
        
        OnDuelActive?.Invoke(_defender, _chosenState);
        
        if (AIManager.Instance.IsAIPlayer(_attacker))
        {
            Debug.Log("is ai player------------------------------------------------------------");
            var aiPlayer = AIManager.Instance.GetAIPlayer(_attacker);
            GameManager.Instance.StartCoroutine(AIManager.Instance.mainAI.ExecuteDuel(aiPlayer));
        }
    }
    
    public void EvaluateCapture(int roll)
    {
        bool success = _chosenState.IsSuccessfulRoll(roll, _attacker.assignedActor.team);
        foreach (var var in _chosenState.redSuccessRolls)
        {
            Debug.Log($"red rolls: {var}");
        }

        Debug.Log($"rolled: {roll}");
           
        if (success)
        {
            SwitchCardHolder(_attacker, _defender, _chosenState);
        }
        else
        {
            _mainPhase.ReturnCardToDeck(_currentEventCard);
            Debug.Log($"Player {_attacker.playerID} failed to capture {_chosenState.cardName}");
        }
        
        OnDuelCompleted?.Invoke();
    }

    private void SwitchCardHolder(Player attacker, Player defender, StateCard chosenState)
    {
        defender.RemoveCapturedCard(chosenState);
        attacker.CaptureCard(chosenState);
        
        _mainPhase.UpdateStateOwnership(attacker, chosenState);
        
        Debug.Log($"Player captured {chosenState.cardName}");
    }

    #endregion Challenge

}