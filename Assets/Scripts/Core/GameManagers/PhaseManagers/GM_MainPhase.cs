using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class GM_MainPhase : GM_BasePhase
{
    public override GamePhase PhaseType => GamePhase.MainGame;
    
    public Card CurrentTargetCard { get; private set; }
    public EventCard CurrentEventCard { get; private set; }

    private readonly Dictionary<Player, EventCard> _heldEvents = new();
    private readonly Dictionary<Card, Player> _cardOwners = new();
    private readonly Dictionary<StateCard, Player> _stateOwners = new();
    private readonly Dictionary<InstitutionCard, Player> _institutionOwners = new();
    
    public Dictionary<StateCard, Player> GetStateOwners() => _stateOwners;

    private readonly List<Card> _mainDeck = new();
    private readonly List<EventCard> _eventDeck = new();

    public EventManager EventManager { get; private set; }
    private AM_MainPhase _aiManager;

    public GM_MainPhase()
    {
        EventManager = new EventManager(this);
        
        BuildAndShuffleDecks();
    }

    protected override void HandleTurnEvent(IGameEvent e)
    {
        base.HandleTurnEvent(e);
        
        if (!isActive) return;

        if (e is MainStageEvent m)
        {
            switch (m.stage)
            {
                case MainStage.DrawEventCardRequest:
                    HandleDrawEventCardRequest();
                    break;
                
                case MainStage.DrawTargetCardRequest:
                    HandleDrawTargetCardRequest();
                    break;
                
                case MainStage.SaveEventCardRequest:
                    SaveEvent((EventCard)m.payload);
                    break;
                
                case MainStage.ApplyEventCardRequest:
                    EventManager.ApplyEvent(game.CurrentPlayer, CurrentEventCard);
                    break;
                    
            }
        }
    }

    protected override void BeginPhase()
    {
        base.BeginPhase();
        
        game.currentPlayerIndex = 0;
        
        _mainDeck.ShuffleInPlace();
        
        //TODO: don't forget to remove this
        AssignTestCardsToPlayers(_mainDeck);
        
        Debug.Log("🟢 Mainphase UI Ready — starting player turns");
        StartPlayerTurn();
    }

    private void BuildAndShuffleDecks()
    {
        _mainDeck.Clear();
        _eventDeck.Clear();

        _mainDeck.AddRange(game.stateDeck);
        _mainDeck.AddRange(game.institutionDeck);
        ShuffleMainDeck();
        
        _eventDeck.AddRange(game.eventDeck);
        ShuffleEventDeck();
    }

    private void ShuffleMainDeck()
    {
        _mainDeck.ShuffleInPlace();
    }

    private void ShuffleEventDeck()
    {
        _eventDeck.ShuffleInPlace();
    }

#region Turn Sequence

protected override void StartPlayerTurn()
    {
        base.StartPlayerTurn();
        
        if (_eventDeck.Count == 0)
        {
            Debug.Log("No more event cards!");
            return;
        }
        
        Player current = game.CurrentPlayer;
        Debug.Log($"--- Player {current.playerID} turn started. Player team: {current.assignedActor.team}---");
        
        if (aiManager.IsAIPlayer(current))
        {
            var aiPlayer = aiManager.GetAIPlayer(current);
            game.StartCoroutine(aiManager.mainAI.ExecuteAITurn(aiPlayer));
        }
    }

    protected override void EndPlayerTurn()
    {
        Player current = game.CurrentPlayer;
        Debug.Log($"--- Player {current.playerID} turn ended ---");
        current.ResetRollCount();
        
        base.EndPlayerTurn();
        
        ClearCurrentEventCard();
        
        MoveToNextPlayer();
    }

    protected override void MoveToNextPlayer()
    {
        game.currentPlayerIndex = (game.currentPlayerIndex + 1) % game.players.Count;
        StartPlayerTurn();
    }

    protected override void HandleRequestedRoll()
    {
        var player = game.CurrentPlayer;
        
        if (!player.CanRoll())
        {
            Debug.Log("Player can't roll.");
            return;
        }
        
        base.HandleRequestedRoll();
        
        player.RegisterRoll();
        
        CheckStateCardConditions(diceRoll, out var discarded);
        
        if (discarded)
        {
            EndPlayerTurn();
            return;
        }
        
        //TODO: maybe register to bus after rolling to see which evaluate to run
        Debug.Log($"Rolled: {diceRoll}");
        EvaluateCapture(player, diceRoll);
        
    }
    
#endregion

#region Card Capture

    private void CheckStateCardConditions(int roll, out bool stateDiscarded)
    {
        if (CurrentTargetCard is StateCard state)
        {
            if (state.hasSecession && roll == 1)
            {
                Debug.Log($"Player {game.CurrentPlayer.playerID} rolled: {roll} and {CurrentTargetCard.cardName} had secession");
                
                DiscardState(state);
                stateDiscarded = true;
                return;
            }

            if (state.hasRollAgain && roll == 4)
            {
                //TODO: bus for adding extra roll for UI visuals
                game.CurrentPlayer.AddExtraRoll();
            }
        }

        stateDiscarded = false;
    }
    
    //TODO: maybe raise a bus after rolling to see if we should run this
    private void EvaluateCapture(Player player, int roll)
    {
       var success = CurrentTargetCard switch
        {
            StateCard s => s.IsSuccessfulRoll(roll, player.assignedActor.team),
            InstitutionCard i => i.IsSuccessfulRoll(roll, player.assignedActor.team),
            _ => false
        };
        
        if (success)
        {
            CaptureCard(player, CurrentTargetCard);
            
            EndPlayerTurn();
        }
        else if (player.CanRoll())
        {
            //TODO: maybe bus for visual indication
            
            Debug.Log("-------- Player can roll again ----------");
            //wait for player to roll again
            if (aiManager.IsAIPlayer(player))
            {
                var aiPlayer = aiManager.GetAIPlayer(player);
                game.StartCoroutine(aiManager.mainAI.RollDice(aiPlayer));
            }
        }
        else
        {
            Debug.Log($"Player {player.playerID} failed to capture {CurrentTargetCard.cardName}");
            EndPlayerTurn();
        }
    }
    public void CaptureCard(Player player, Card card)
    {
        if (card == null)
            return;

        // Avoid capturing a card that’s already held
        if (IsCardHeld(card))
        {
            Debug.LogWarning($"Attempted to capture {card.cardName}, but it's already held by {GetCardHolder(card)?.playerID}.");
            return;
        }
        
        card.isCaptured = true;

        // Add to the correct collection
        switch (card)
        {
            case StateCard stateCard:
                _stateOwners.TryAdd(stateCard, player);
                break;

            case InstitutionCard institutionCard:
                _institutionOwners.TryAdd(institutionCard, player);
                break;
        }

        // Keep reverse lookup in sync
        _cardOwners[card] = player;

        // Remove from main deck if still present
        _mainDeck.Remove(card);

        // Update player data
        player.CaptureCard(card);

        Debug.Log($"Player {player.playerID} captured {card.cardName}");

        //TODO: pay attention to this, since it's raised after removing the card from the deck, UI might not be able to find it
        TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.CardCaptured, new CardCapturedData(player, card)));

        ClearCurrentTargetCard();
    }
    
    //TODO: should we have bus here
    //TODO: does it make sense to have updatecardownership bus, because capture card has it for ui too, maybe need a bool param there
    private void UncaptureCard(Card card, bool returnToDeck = false)
    {
        if (card == null || !IsCardHeld(card))
            return;

        card.isCaptured = false;

        // Get the card's current owner using our helper
        Player owner = GetCardHolder(card);
        if (owner == null)
            return;

        // Remove from owner’s data
        owner.RemoveCapturedCard(card);

        // Remove from type-specific collection
        switch (card)
        {
            case StateCard stateCard:
                _stateOwners.Remove(stateCard);
                break;

            case InstitutionCard institutionCard:
                _institutionOwners.Remove(institutionCard);
                break;
        }

        // Remove from reverse lookup
        _cardOwners.Remove(card);

        if (returnToDeck)
            ReturnCardToDeck(card);
    }
    
    public void UpdateCardOwnership(Player newOwner, Card card)
    {
        // Remove card from its current owner first
        UncaptureCard(card);

        // Add to new owner's list
        CaptureCard(newOwner, card);
        
        //This is only here to notify the UI
        Player owner = GetCardHolder(card);
        
        TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.CardOwnerChanged, new CardOwnerChangedData(owner, newOwner, card)));
        
    }
    
    public void DiscardState(StateCard stateToDiscard)
    {
        if (_mainDeck.Contains(stateToDiscard))
            _mainDeck.Remove(stateToDiscard);
        
        if (CurrentTargetCard == stateToDiscard)
            CurrentTargetCard = null;
        
        TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.StateDiscarded, stateToDiscard));
    }

    private void ClearCurrentTargetCard()
    {
        if (CurrentTargetCard == null) return;
        
        //TODO: check if needed
        // TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.CurrentTargetCardCleared, CurrentTargetCard));
        
        CurrentTargetCard = null;
    }
#endregion    

#region Event Card

    private void HandleDrawEventCardRequest()
    {
        if (_eventDeck.Count == 0) return;

        EventCard card = _eventDeck.PopFront();
        
        Debug.Log($"Draw event card: {card.cardName}");

        CurrentEventCard = card;
        
        TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.EventCardDrawn, card));
        
    }
    
    private void SaveEvent(EventCard card)
    {
        if (card != CurrentEventCard)
        {
            Debug.LogWarning($"Tried to save {card.cardName} but current event is {CurrentEventCard.cardName}");
            return;
        }
        var player = game.CurrentPlayer;

        if (_heldEvents.ContainsKey(player))
            return;

        _heldEvents[player] = card;
        player.SaveEvent(card);
        Debug.Log($"Saved {card.cardName}");
        
        TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.EventCardSaved, CurrentEventCard));
        
        ClearCurrentEventCard();
    }
    
    private void ClearCurrentEventCard()
    {
        if (CurrentEventCard == null) return;
        
        //TODO: check if needed
        // TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.CurrentEventCardCleared, CurrentEventCard));
        CurrentEventCard = null;
    }
    
    
#endregion
    
#region Target Card

    private void HandleDrawTargetCardRequest()
    {
        if (_mainDeck.Count == 0)
        {
            Debug.LogWarning("Main deck empty!");
            return;
        }

        Card card = _mainDeck.PopFront();
        
        Debug.Log($"Draw target card: {card.cardName}");

        CurrentTargetCard = card;
        
        TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.TargetCardDrawn, card));
        
    }
    
    public void ReturnCardToDeck(Card card)
    {
        switch (card)
        {
            case EventCard eventCard:
                _eventDeck.Insert(UnityEngine.Random.Range(0, _eventDeck.Count + 1), eventCard);
                //No need to clear the card here because when the event card is played it's cleared already
                break;
            
            case StateCard stateCard:
                //TODO:uncapture also?
                _mainDeck.Insert(UnityEngine.Random.Range(0, _mainDeck.Count + 1), stateCard);
                //TODO: do we need to clear the card here?
                break;
            
            case InstitutionCard institutionCard:
                //TODO:uncapture also?
                _mainDeck.Insert(UnityEngine.Random.Range(0, _mainDeck.Count + 1), institutionCard);
                //TODO: do we need to clear the card here?
                break;
        }
        
        TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.CardReturnedToDeck, card));
    }
    
    public Player GetCardHolder(Card card)
    {
        if (card == null) return null;
        
        if (!IsCardHeld(card)) return null;
        
        return _cardOwners.GetValueOrDefault(card);
    }

    private bool IsCardHeld(Card card)
    {
        return card != null && _cardOwners.ContainsKey(card);
    }
    
    public InstitutionCard FindHeldInstitution(InstitutionCard requiredInst, out bool cardFound)
    {
        if (requiredInst == null)
        {
            cardFound = false;
            return null;
        }
        
        foreach (var kvp in _institutionOwners)
        {
            var inst = kvp.Key;
            
            // Match by cardName (or by a unique ID if you have one)
            if (inst.cardName == requiredInst.cardName)
            {
                cardFound = true;
                return inst;
            }
        }

        cardFound = false;
        return null;
    }
    
    public StateCard FindStateFromDeck(StateCard state, out bool cardFound)
    {
        if (state == null)
        {
            cardFound = false;
            return null;
        }
        
        foreach (var card in _mainDeck)
        {
            // Match by cardName (or by a unique ID if you have one)
            if (card.cardName == state.cardName)
            {
                cardFound = true;
                return card as StateCard;
            }
        }

        cardFound = false;
        return null;
    }

    #endregion    

#region Test helpers
    /// <summary>
    /// Assigns a few cards from the deck to each player for testing or setup purposes.
    /// </summary>
    /// <param name="cardsPerPlayer">How many cards each player should receive.</param>
    private void AssignTestCardsToPlayers<T>(List<T> sourceDeck, int cardsPerPlayer = 2) where T : Card
    {
        if (game.players == null || game.players.Count == 0)
        {
            Debug.LogWarning("No players available for card assignment.");
            return;
        }

        if (sourceDeck == null || sourceDeck.Count == 0)
        {
            Debug.LogWarning("No cards available for assignment.");
            return;
        }

        // Create a shuffled copy of the deck
        var shuffled = sourceDeck.Shuffled();

        // 🔹 Force assignment for CIA and Supreme Court if present
        var ciaCard = shuffled.FirstOrDefault(c => c.cardName.Contains("CIA", StringComparison.OrdinalIgnoreCase));
        var courtCard = shuffled.FirstOrDefault(c => c.cardName.Contains("Supreme", StringComparison.OrdinalIgnoreCase));

        if (ciaCard != null)
        {
            shuffled.Remove(ciaCard);
            CaptureCard(game.players[5], ciaCard); // assign CIA to Player 0
            Debug.Log($"🔸 Assigned CIA to {game.players[5].PlayerName}");
        }

        if (courtCard != null)
        {
            shuffled.Remove(courtCard);
            var targetIndex = 5; // if 2+ players, give to Player 1
            CaptureCard(game.players[targetIndex], courtCard);
            Debug.Log($"🔸 Assigned Supreme Court to {game.players[targetIndex].PlayerName}");
        }

        // Continue assigning random remaining cards
        int index = 0;
        foreach (var player in game.players)
        {
            for (int i = 0; i < cardsPerPlayer; i++)
            {
                if (index >= shuffled.Count)
                {
                    Debug.LogWarning("Not enough cards to assign evenly.");
                    Debug.Log("✅ Partial test assignment completed.");
                    return;
                }

                var card = shuffled[index++];

                // Skip if we already assigned CIA or Supreme Court to this player
                if (card.cardName.Contains("CIA", StringComparison.OrdinalIgnoreCase) ||
                    card.cardName.Contains("Supreme", StringComparison.OrdinalIgnoreCase))
                    continue;

                CaptureCard(player, card);
            }
        }

        Debug.Log("✅ Test card assignment completed.");
    }

    
#endregion
}
