using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EventManager
{
    private readonly MainPhaseGameManager _mainPhase;
    private readonly Dictionary<EventType, Action<Player, EventCard>> _handlers;
    public event Action<EventCard> OnEventApplied;
    public MonoBehaviour activeEventUI;

    private bool _needTwoActive = false;
    private bool _eventActive = false;
    public bool IsEventActive => _eventActive;
    
    private EventCard _currentEventCard;

    public EventManager(MainPhaseGameManager gm)
    {
        _mainPhase = gm;
        _handlers = new()
        {
            { EventType.ExtraRoll, HandleExtraRoll },
            { EventType.NeedTwo, HandleNeedTwo },
            { EventType.LoseTurn, HandleLoseTurn },
            { EventType.AlternativeStates, HandleAlternativeStates },
            { EventType.Challenge, HandleChallenge },
            { EventType.NoImpact, (p, c) => {} }
        };
        
    }

    
    public void ApplyEvent(Player player, EventCard card)
    {
        Debug.Log($"Applying event {card.cardName}");
        EventType effectiveType = card.eventType;

        if (card.eventType == EventType.TeamBased)
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
        bool canApply = card.eventConditions switch
        {
            EventConditions.IfOwnsInstitution => player.HasInstitution(card.requiredInstitution),
            EventConditions.None => true,
            _ => false
        };

        if (canApply)
            player.AddExtraRoll();
        else if (card.canReturnToDeck)
            _mainPhase.ReturnCardToDeck(card);
    }
    
#endregion

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
#endregion

#region Lose Turn

    private void HandleLoseTurn(Player player, EventCard card)
    {
        _mainPhase.EndPlayerTurn();
    }
    
#endregion

#region Alternative States

    private StateCard _altState1;
    private StateCard _altState2;
    private Player _currentPlayer;
    public event Action<Player, StateCard, StateCard> OnAltStatesActive;
    public event Action OnAltStatesCompleted;

    private void HandleAlternativeStates(Player player, EventCard card)
    {
        _altState1 = _mainPhase.FindStateFromDeck(card.altState1, out var found1);
        _altState2 = _mainPhase.FindStateFromDeck(card.altState1, out var found2);

        if (found1 || found2)
        {
            _eventActive = true;
            _currentPlayer = player;
            _currentEventCard = card;
            OnAltStatesActive?.Invoke(player, _altState1, _altState2);
            
            RollDiceForAI();
        }
        else
        {
            CancelEvent(card);
        }
    }
    
    public void EvaluateStateDiscard(int roll)
    {
        StateCard cardToDiscard = null;
        
        switch (roll)
        {
            case 1:
                if (_altState1 != null)
                {
                    cardToDiscard = _altState1;
                }
                break;
            case 2:
                if (_altState2 != null)
                {
                    cardToDiscard = _altState2;
                }
                break;
        }

        if (cardToDiscard != null)
        {
            _mainPhase.DiscardState(cardToDiscard);
        }
        else
        {
            Debug.Log($"Player {_currentPlayer.playerID} didn't discard any states!");
        }

        OnAltStatesCompleted?.Invoke();
        
        NullifyVariables();
        _mainPhase.EndPlayerTurn();   
    }

#endregion
    
#region Challenge

    public event Action<List<StateCard>> OnChallengeState;
    public event Action<Player, Card> OnDuelActive;
    public event Action OnDuelCompleted;
    
    private Card _chosenCard;
    private Player _defender;

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
        }
    }
    
    public void EvaluateChallengeCapture(int roll)
    {
        bool success = _chosenCard switch
        {
            StateCard s => s.IsSuccessfulRoll(roll, _currentPlayer.assignedActor.team),
            InstitutionCard i => i.IsSuccessfulRoll(roll, _currentPlayer.assignedActor.team),
            _ => false
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
        
        OnDuelCompleted?.Invoke();
        
        NullifyVariables();
        _mainPhase.EndPlayerTurn();
        
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
        
        if (cardHolder != player)
        {
            _defender = cardHolder;
        }
        
        if (!_defender)
        {
            CancelEvent(card);
            return;
        }
        
        _eventActive = true;
        
        OnDuelActive?.Invoke(player, _chosenCard);
        
        RollDiceForAI();
        
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
            
        OnChallengeState?.Invoke(availableStates);
            
        if (AIManager.Instance.IsAIPlayer(player))
        {
            var aiPlayer = AIManager.Instance.GetAIPlayer(player);
            GameManager.Instance.StartCoroutine(AIManager.Instance.mainAI.ExecuteChooseState(aiPlayer, availableStates));
        }
            
        StateDisplayCard.OnCardHeld += HandleStateChosen;

        _eventActive = true;
    }
    
    private List<StateCard> GetChallengableStatesForPlayer(Player player)
    {
        var stateOwners = _mainPhase.GetStateOwners();
        var availableStates = new List<StateCard>();

        // Get each state that the current player doesn't own
        foreach (var kvp in stateOwners)
        {
            if (kvp.Value == player)
                continue;
            
            availableStates.Add(kvp.Key);
        }

        return availableStates.Count == 0 ? null : availableStates;
    }

    //Set to public for AI
    public void HandleStateChosen(StateCard chosenState)
    {
        StateDisplayCard.OnCardHeld -= HandleStateChosen;
        
        _chosenCard = chosenState;
        
        Debug.Log($"Player held {_chosenCard.cardName} → set as challenge target.");

        _defender = _mainPhase.GetCardHolder(_chosenCard);
        
        OnDuelActive?.Invoke(_defender, _chosenCard);
        
        
    }

#endregion

#region Helper Methods

    void CancelEvent(EventCard card)
    {
        Debug.Log("Challenge cannot be applied.");
        if (card.canReturnToDeck)
        {
            _mainPhase.ReturnCardToDeck(card);
        }
        NullifyVariables();
    }
    private void NullifyVariables()
    {
        _chosenCard = null;
        _defender = null;
        _altState1 = null;
        _altState2 = null;
        _currentPlayer = null;
        activeEventUI = null;
        
        _eventActive = false;
    }

    private void RollDiceForAI()
    {
        if (!AIManager.Instance.IsAIPlayer(_currentPlayer)) return;
        
        var aiPlayer = AIManager.Instance.GetAIPlayer(_currentPlayer);
        GameManager.Instance.StartCoroutine(AIManager.Instance.mainAI.RollDice(aiPlayer, activeEventUI));
    }

#endregion
   

}