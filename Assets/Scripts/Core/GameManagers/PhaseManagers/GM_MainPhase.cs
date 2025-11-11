using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

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

    // === Events for UI or external systems ===
    public event Action<Player, Card> OnCardCaptured;
    public event Action<StateCard> OnStateDiscarded;
    private Action<EventCard> _eventAppliedHandler;

    public GM_MainPhase()
    {
        EventManager = new EventManager(this);
        
        BuildAndShuffleDecks();
    }
    
    protected override void BeginPhase()
    {
        base.BeginPhase();
        _eventAppliedHandler = _ => ClearEventCard();
        EventManager.OnEventApplied += _eventAppliedHandler;
        game.currentPlayerIndex = 0;
        
        var ui = GameUIManager.Instance.mainUI;
        if (ui)
        {
            ui.OnUIReady = () =>
            {
                Debug.Log("🟢 Mainphase UI Ready — starting player turns");
                StartPlayerTurn();
            };
        }
    }

    protected override void EndPhase()
    {
        base.EndPhase();
        if (_eventAppliedHandler != null)
            EventManager.OnEventApplied -= _eventAppliedHandler;
    }

    private void BuildAndShuffleDecks()
    {
        _mainDeck.Clear();
        _eventDeck.Clear();

        _mainDeck.AddRange(game.stateDeck);
        _mainDeck.AddRange(game.institutionDeck);
        _mainDeck.ShuffleInPlace();

        _eventDeck.AddRange(game.eventDeck.Shuffled());
    }

#region Turn Sequence
    public override void StartPlayerTurn()
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

    public override void EndPlayerTurn()
    {
        Player current = game.CurrentPlayer;
        Debug.Log($"--- Player {current.playerID} turn ended ---");
        current.ResetRollCount();
        
        base.EndPlayerTurn();
        
        ClearEventCard();
        
        MoveToNextPlayer();
    }

    public override void MoveToNextPlayer()
    {
        game.currentPlayerIndex = (game.currentPlayerIndex + 1) % game.players.Count;
        StartPlayerTurn();
    }

    public override void PlayerRolledDice(int roll)
    {
        Player current = game.CurrentPlayer;
        if (!current.CanRoll())
        {
            Debug.Log("Player already rolled.");
            return;
        }

        current.RegisterRoll();
        
        Debug.Log($"Rolled: {roll}");
        EvaluateCapture(current, roll);
    }
    
#endregion

#region Card Capture

    public void CheckStateCardConditions(int roll, out bool stateDiscarded)
    {
        if (CurrentTargetCard is StateCard state)
        {
            if (state.hasSecession && roll == 1)
            {
                Debug.Log($"Player {game.CurrentPlayer.playerID} rolled: {roll} and {CurrentTargetCard.cardName} had secession");
                
                //TODO: Fire an event for UI and then call this from UI
                DiscardState(state);
                stateDiscarded = true;
                return;
            }

            if (state.hasRollAgain && roll == 4)
            {
                //TODO: move add extraroll logic to here and fire event for UI to indicate
                game.CurrentPlayer.AddExtraRoll();
            }
        }

        stateDiscarded = false;
    }
    private void EvaluateCapture(Player player, int roll)
    {
        CheckStateCardConditions(roll, out var discarded);

        if (discarded)
        {
            EndPlayerTurn();
            return;
        }
        
        bool success;
            
        if (EventManager.ConsumeNeedTwo())
        {
            success = (roll == 2);
            Debug.Log($"'Need2' active — success = {success}");
            player.RegisterRoll();
        }
        else
        {
            success = CurrentTargetCard switch
            {
                StateCard s => s.IsSuccessfulRoll(roll, player.assignedActor.team),
                InstitutionCard i => i.IsSuccessfulRoll(roll, player.assignedActor.team),
                _ => false
            };
        }

        if (success)
        {
            //TODO: call this from UI
            CaptureCard(player, CurrentTargetCard);
            
            OnCardCaptured?.Invoke(player, CurrentTargetCard);
            
            CurrentTargetCard = null;
            EndPlayerTurn();
        }
        else if (player.CanRoll())
        {
            Debug.Log("-------- Player can roll again ----------");
            //wait for player to roll again
            if (aiManager.IsAIPlayer(player))
            {
                var aiPlayer = aiManager.GetAIPlayer(player);
                game.StartCoroutine(aiManager.mainAI.ExecuteAITurn(aiPlayer));
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
    }
    
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
        
        //TODO:indicate this in UI
        
        // Remove card from its current owner first
        UncaptureCard(card);

        // Add to new owner's list
        CaptureCard(newOwner, card);
    }

    public void DiscardState(StateCard stateToDiscard)
    {
        //TODO:indicate this in UI
        
        if (_mainDeck.Contains(stateToDiscard))
        {
            Debug.Log($"{stateToDiscard.cardName} was discarded");
            _mainDeck.Remove(stateToDiscard);
        }
        else if (CurrentTargetCard == stateToDiscard)
        {
            OnStateDiscarded?.Invoke(stateToDiscard);
            CurrentTargetCard = null;
        }
        
    }
    
#endregion    

#region Event Card
    public EventCard DrawEventCard()
    {
        if (_eventDeck.Count == 0) return null;

        EventCard card = _eventDeck.PopFront();
        
        Debug.Log($"Draw event card: {card.cardName}");

        CurrentEventCard = card;
        
        return card;
    }

    public bool TrySaveEvent(EventCard card)
    {
        var player = game.CurrentPlayer;

        if (_heldEvents.ContainsKey(player))
            return false;

        _heldEvents[player] = card;
        player.SaveEvent(card);
        Debug.Log($"Saved {card.cardName}");
        
        ClearEventCard();
        
        return true;
    }
    
    private void ClearEventCard()
    {
        CurrentEventCard = null;
    }
    
    
#endregion
    
#region Target Card

    public Card DrawTargetCard()
    {
        if (_mainDeck.Count == 0)
        {
            Debug.LogWarning("Main deck empty!");
            return null;
        }

        Card drawn = _mainDeck.PopFront();
        
        Debug.Log($"Draw target card: {drawn.cardName}");

        CurrentTargetCard = drawn;
        
        return drawn;
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
        
        Debug.Log($"Returned to deck {card.cardName}");
        
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

        // Create a shuffled copy of the card deck
        var shuffled = sourceDeck.Shuffled();

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

                // Use your proper capture logic — keeps everything in sync
                CaptureCard(player, card);

            }
        }

        Debug.Log("✅ Test state assignment completed.");
    }
    
#endregion
}
