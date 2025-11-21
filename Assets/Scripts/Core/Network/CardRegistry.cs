using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages card instances and their unique IDs for network synchronization.
/// Converts between Card objects (local) and card IDs (networked).
/// </summary>
public class CardRegistry
{
    private static CardRegistry _instance;
    public static CardRegistry Instance => _instance ??= new CardRegistry();
    
    // Card ID -> Card instance lookups
    private readonly Dictionary<string, StateCard> _stateCards = new();
    private readonly Dictionary<string, InstitutionCard> _institutionCards = new();
    private readonly Dictionary<string, EventCard> _eventCards = new();
    private readonly Dictionary<string, ActorCard> _actorCards = new();
    
    // Reverse lookup: Card -> ID
    private readonly Dictionary<Card, string> _cardToId = new();
    
    private int _nextCardId = 0;
    
    /// <summary>
    /// Register a card at runtime (from already-loaded decks)
    /// </summary>
    public void RegisterCardRuntime(Card card)
    {
        if (card == null) return;
        if (_cardToId.ContainsKey(card)) return; // Already registered
        
        string prefix = card switch
        {
            StateCard => "STATE",
            InstitutionCard => "INST",
            EventCard => "EVENT",
            ActorCard => "ACTOR",
            _ => "CARD"
        };
        
        string id = GenerateCardId(prefix);
        RegisterCard(id, card);
    }
    
    /// <summary>
    /// Initialize the registry with all cards from the game deck.
    /// Call this once at game start.
    /// </summary>
    public void InitializeFromGameDeck(GameDeckSO gameDeck)
    {
        Clear();
        
        // Register all states
        foreach (var stateSO in gameDeck.allStates)
        {
            var card = stateSO.ToCard();
            string id = GenerateCardId("STATE");
            RegisterCard(id, card);
        }
        
        // Register all institutions
        foreach (var instSO in gameDeck.allInstitutions)
        {
            var card = instSO.ToCard();
            string id = GenerateCardId("INST");
            RegisterCard(id, card);
        }
        
        // Register all events - build the list first
        var allEvents = new List<EventCardSO>();
        allEvents.AddRange(gameDeck.extraRollEvents);
        allEvents.AddRange(gameDeck.needTwoEvents);
        allEvents.AddRange(gameDeck.challengeEvents);
        allEvents.AddRange(gameDeck.loseTurnEvents);
        allEvents.AddRange(gameDeck.noImpactEvents);
        allEvents.AddRange(gameDeck.alternativeStatesEvents);
        allEvents.AddRange(gameDeck.teamBasedEvents);
        
        foreach (var eventSO in allEvents)
        {
            var card = eventSO.ToCard();
            string id = GenerateCardId("EVENT");
            RegisterCard(id, card);
        }
        
        // Register all actors
        foreach (var actorSO in gameDeck.allActors)
        {
            var card = actorSO.ToCard();
            string id = GenerateCardId("ACTOR");
            RegisterCard(id, card);
        }
        
        Debug.Log($"CardRegistry initialized: {_stateCards.Count} states, {_institutionCards.Count} institutions, {_eventCards.Count} events, {_actorCards.Count} actors");
    }
    
    private string GenerateCardId(string prefix)
    {
        return $"{prefix}_{_nextCardId++}";
    }
    
    private void RegisterCard(string id, Card card)
    {
        switch (card)
        {
            case StateCard state:
                _stateCards[id] = state;
                break;
            case InstitutionCard inst:
                _institutionCards[id] = inst;
                break;
            case EventCard evt:
                _eventCards[id] = evt;
                break;
            case ActorCard actor:
                _actorCards[id] = actor;
                break;
        }
        
        _cardToId[card] = id;
    }
    
    /// <summary>
    /// Get card ID from card instance
    /// </summary>
    public string GetCardId(Card card)
    {
        if (card == null) return null;
        return _cardToId.TryGetValue(card, out var id) ? id : null;
    }
    
    /// <summary>
    /// Get card instance from ID
    /// </summary>
    public Card GetCard(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return null;
        
        if (_stateCards.TryGetValue(cardId, out var state))
            return state;
        if (_institutionCards.TryGetValue(cardId, out var inst))
            return inst;
        if (_eventCards.TryGetValue(cardId, out var evt))
            return evt;
        if (_actorCards.TryGetValue(cardId, out var actor))
            return actor;
            
        return null;
    }
    
    public StateCard GetStateCard(string cardId)
    {
        return _stateCards.TryGetValue(cardId, out var card) ? card : null;
    }
    
    public InstitutionCard GetInstitutionCard(string cardId)
    {
        return _institutionCards.TryGetValue(cardId, out var card) ? card : null;
    }
    
    public EventCard GetEventCard(string cardId)
    {
        return _eventCards.TryGetValue(cardId, out var card) ? card : null;
    }
    
    public ActorCard GetActorCard(string cardId)
    {
        return _actorCards.TryGetValue(cardId, out var card) ? card : null;
    }
    
    /// <summary>
    /// Convert a list of cards to IDs
    /// </summary>
    public List<string> GetCardIds<T>(List<T> cards) where T : Card
    {
        var ids = new List<string>();
        foreach (var card in cards)
        {
            var id = GetCardId(card);
            if (id != null)
                ids.Add(id);
        }
        return ids;
    }
    
    /// <summary>
    /// Convert a list of IDs to cards
    /// </summary>
    public List<T> GetCards<T>(List<string> cardIds) where T : Card
    {
        var cards = new List<T>();
        foreach (var id in cardIds)
        {
            var card = GetCard(id);
            if (card is T typedCard)
                cards.Add(typedCard);
        }
        return cards;
    }
    
    public void Clear()
    {
        _stateCards.Clear();
        _institutionCards.Clear();
        _eventCards.Clear();
        _actorCards.Clear();
        _cardToId.Clear();
        _nextCardId = 0;
    }
}

/// <summary>
/// Extension methods to make working with cards and IDs easier
/// </summary>
public static class CardRegistryExtensions
{
    public static string ToId(this Card card)
    {
        return CardRegistry.Instance.GetCardId(card);
    }
    
    public static Card ToCard(this string cardId)
    {
        return CardRegistry.Instance.GetCard(cardId);
    }
    
    public static List<string> ToIds<T>(this List<T> cards) where T : Card
    {
        return CardRegistry.Instance.GetCardIds(cards);
    }
    
    public static List<T> ToCards<T>(this List<string> ids) where T : Card
    {
        return CardRegistry.Instance.GetCards<T>(ids);
    }
}