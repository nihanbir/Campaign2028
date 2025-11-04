using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class MainPhaseGameManager : BasePhaseGameManager
{
    private Card _currentTargetCard;
    private EventCard _currentEventCard;

    private readonly Dictionary<Player, EventCard> _heldEvents = new();
    private readonly Dictionary<Card, Player> _cardOwners = new();
    private readonly Dictionary<StateCard, Player> _stateOwners = new();
    private readonly Dictionary<InstitutionCard, Player> _institutionOwners = new();
    
    public Dictionary<StateCard, Player> GetStateOwners() => _stateOwners;

    private readonly List<Card> _mainDeck = new();
    private readonly List<EventCard> _eventDeck = new();

    public EventManager EventManager { get; private set; }
    private AIManager _aiManager;

    // === Events for UI or external systems ===
    public event Action<Player, Card> OnCardCaptured;
    public event Action<Player> OnPlayerTurnStarted;
    public event Action<Player> OnPlayerTurnEnded;
    public event Action<EventCard> OnCardSaved;

    public MainPhaseGameManager(GameManager gm) : base(gm)
    {
        EventManager = new EventManager(this);
    }
    
    public override void InitializePhase()
    {
        Debug.Log("=== MAIN PHASE START ===");

        _aiManager = AIManager.Instance;
        _aiManager.mainAI.InitializeAIManager();
        EventManager.OnEventApplied += _ => ClearEventCard();
        
        BuildAndShuffleDecks();
        
        //TODO: dont forget to remove
        AssignTestCardsToPlayers(game.institutionDeck, 1);
        // AssignTestCardsToPlayers(game.stateDeck, 1);
        
        game.currentPlayerIndex = 0;
        StartPlayerTurn();
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
        if (_eventDeck.Count == 0)
        {
            Debug.Log("No more event cards!");
            return;
        }
        
        Player current = game.CurrentPlayer;
        Debug.Log($"--- Player {current.playerID} turn started. Player team: {current.assignedActor.team}---");
        
        //This needs to be invoked before drawing a card to set _isPlayerAI correctly in UImanager
        OnPlayerTurnStarted?.Invoke(current);

        _currentTargetCard ??= DrawTargetCard();

        _currentEventCard ??= DrawEventCard();
        
        if (_aiManager.IsAIPlayer(current))
        {
            var aiPlayer = _aiManager.GetAIPlayer(current);
            game.StartCoroutine(_aiManager.mainAI.ExecuteAITurn(aiPlayer, _currentEventCard));
        }
    }

    public override void EndPlayerTurn()
    {
        Player current = game.CurrentPlayer;
        Debug.Log($"--- Player {current.playerID} turn ended ---");
        current.ResetRollCount();
        OnPlayerTurnEnded?.Invoke(current);
        ClearEventCard();
        
        MoveToNextPlayer();
    }

    public override void MoveToNextPlayer()
    {
        game.currentPlayerIndex = (game.currentPlayerIndex + 1) % game.players.Count;
        StartPlayerTurn();
    }

    public override void PlayerRolledDice()
    {
        Player current = game.CurrentPlayer;
        if (!current.CanRoll())
        {
            Debug.Log("Player already rolled.");
            return;
        }

        current.RegisterRoll();
        
        int roll = GameUIManager.Instance.DiceRoll;
        Debug.Log($"Rolled: {roll}");
        EvaluateCapture(current, roll);
    }
    
#endregion

#region Card Capture
    private void EvaluateCapture(Player player, int roll)
    {
        bool success;
            
        if (EventManager.ConsumeNeedTwo())
        {
            success = (roll == 2);
            Debug.Log($"'Need2' active — success = {success}");
        }
        else
        {
            success = _currentTargetCard switch
            {
                StateCard s => s.IsSuccessfulRoll(roll, player.assignedActor.team),
                InstitutionCard i => i.IsSuccessfulRoll(roll, player.assignedActor.team),
                _ => false
            };
        }

        if (success)
        {
            CaptureCard(player, _currentTargetCard);
            
            OnCardCaptured?.Invoke(player, _currentTargetCard);
            _currentTargetCard = null;
            EndPlayerTurn();
        }
        else if (player.CanRoll())
        {
            Debug.Log("-------- Player can roll again ----------");
            //wait for player to roll again
            if (_aiManager.IsAIPlayer(player))
            {
                var aiPlayer = _aiManager.GetAIPlayer(player);
                game.StartCoroutine(_aiManager.mainAI.ExecuteAITurn(aiPlayer, _currentEventCard));
            }
        }
        else
        {
            Debug.Log($"Player {player.playerID} failed to capture {_currentTargetCard.cardName}");
            EndPlayerTurn();
        }
    }
    private void CaptureCard(Player player, Card card)
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
            // Remove card from its current owner first
            UncaptureCard(card);
    
            // Add to new owner's list
            CaptureCard(newOwner, card);
        }
    
#endregion    

#region Event Card
    private EventCard DrawEventCard()
    {
        Debug.Log("Draw event card");
        if (_eventDeck.Count == 0) return null;

        EventCard card = _eventDeck.PopFront();
        
        Debug.Log($"{card.cardName}");
        
        //TODO: add event
        GameUIManager.Instance.mainUI.SpawnEventCard(card);
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
        OnCardSaved?.Invoke(card);
        ClearEventCard();
        
        return true;
    }
    
    private void ClearEventCard()
        {
            _currentEventCard = null;
        }
    
    
#endregion
    
#region Target Card

    private Card DrawTargetCard()
        {
            Debug.Log("Draw target card");
            if (_mainDeck.Count == 0)
            {
                Debug.LogWarning("Main deck empty!");
                return null;
            }
    
            Card drawn = _mainDeck.PopFront();
            
            //TODO: add event
            GameUIManager.Instance.mainUI.SpawnTargetCard(drawn);
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

// === Extensions for lists ===
public static class ListExtensions
{
    public static void ShuffleInPlace<T>(this IList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public static List<T> Shuffled<T>(this IEnumerable<T> source)
        => source.OrderBy(_ => UnityEngine.Random.value).ToList();

    public static T PopFront<T>(this IList<T> list)
    {
        T value = list[0];
        list.RemoveAt(0);
        return value;
    }
}
