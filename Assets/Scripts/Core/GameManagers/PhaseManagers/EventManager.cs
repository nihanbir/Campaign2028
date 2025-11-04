using System;
using System.Collections.Generic;
using UnityEngine;

public class EventManager
{
    private readonly MainPhaseGameManager _mainPhase;
    private readonly Dictionary<EventType, Action<Player, EventCard>> _handlers;
    public event Action<EventCard> OnEventApplied;

    private bool _needTwoActive = false;
    private bool _challengeActive = false;
    public bool IsChallengeActive => _challengeActive;
    
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
        switch (card.eventConditions)
        {
            case EventConditions.Any:
                ChallengeAnyState(player, card);
                break;
                
        }
    }
    
    //TODO: change it for card instead of state later
    public void EvaluateCapture(int roll)
    {
        bool success = _chosenState.IsSuccessfulRoll(roll, _attacker.assignedActor.team);

        Debug.Log($"rolled: {roll}");
           
        if (success)
        {
            _mainPhase.UpdateCardOwnership(_attacker, _chosenState);
        }
        else
        {
            _mainPhase.ReturnCardToDeck(_currentEventCard);
            Debug.Log($"Player {_attacker.playerID} failed to capture {_chosenState.cardName}");
        }

        _challengeActive = false;
        OnDuelCompleted?.Invoke();
    }
    
    void CancelChallenge(EventCard card)
        {
            Debug.Log("Challenge cannot be applied.");
            if (card.canReturnToDeck)
            {
                _mainPhase.ReturnCardToDeck(card);
            }
            _challengeActive = false;
        }

#endregion Challenge

#region Challenge Any State

private void ChallengeAnyState(Player player, EventCard card)
    {
        var availableStates = GetChallengableStatesForPlayer(player);
        if (availableStates == null)
        {
            CancelChallenge(card);
            return;
        }
        
        // Here for easy testing
        // GameManager.Instance.currentPlayerIndex = GameManager.Instance.players.FindIndex(p => p.playerID == 0);
            
        _attacker = GameManager.Instance.CurrentPlayer;
            
        OnChallengeState?.Invoke(availableStates);
            
        if (AIManager.Instance.IsAIPlayer(player))
        {
            var aiPlayer = AIManager.Instance.GetAIPlayer(player);
            GameManager.Instance.StartCoroutine(AIManager.Instance.mainAI.ExecuteChooseState(aiPlayer, availableStates));
        }
            
        StateDisplayCard.OnCardHeld += HandleStateChosen;

        _challengeActive = true;
    }
    
    private List<StateCard> GetChallengableStatesForPlayer(Player player)
    {
        var heldStates = _mainPhase.GetHeldStates();
        var availableStates = new List<StateCard>();

        // Get each state that the current player doesn't own
        foreach (var kvp in heldStates)
        {
            if (kvp.Key == player)
                continue;

            foreach (var state in kvp.Value)
            {
                availableStates.Add(state);
            }
        }

        return availableStates.Count == 0 ? null : availableStates;
    }

    //Set to public for AI
    public void HandleStateChosen(StateCard chosenState)
    {
        StateDisplayCard.OnCardHeld -= HandleStateChosen;
        
        _chosenState = chosenState;
        
        Debug.Log($"Player held {_chosenState.cardName} → set as challenge target.");

        _defender = _mainPhase.GetCardHolder(_chosenState);
        
        OnDuelActive?.Invoke(_defender, _chosenState);
        
        if (AIManager.Instance.IsAIPlayer(_attacker))
        {
            var aiPlayer = AIManager.Instance.GetAIPlayer(_attacker);
            GameManager.Instance.StartCoroutine(AIManager.Instance.mainAI.ExecuteDuel(aiPlayer));
        }
    }

#endregion

}