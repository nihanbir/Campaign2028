using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class MainPhaseGameManager : BasePhaseGameManager
{
    private Card _currentTargetCard;
    private EventCard _currentEventCard;

    private readonly Dictionary<Player, EventCard> _heldEvents = new();
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
        EventManager.OnEventApplied += _ =>ClearEventCard();
        
        BuildAndShuffleDecks();
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

    public override void StartPlayerTurn()
    {
        if (_eventDeck.Count == 0)
        {
            Debug.Log("No more event cards!");
            return;
        }
        
        Player current = game.CurrentPlayer;
        Debug.Log($"--- Player {current.playerID} turn started ---");
        
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
            player.CaptureCard(_currentTargetCard);
            OnCardCaptured?.Invoke(player, _currentTargetCard);
            Debug.Log($"Player captured {_currentTargetCard.cardName}");
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

    public void ReturnCardToDeck(Card card)
    {
        if (card is not EventCard eventCard) return;

        _eventDeck.Insert(UnityEngine.Random.Range(0, _eventDeck.Count + 1), eventCard);
        Debug.Log($"Returned to deck {card.cardName}");
        
        //No need to clear the card here because when the event card is played it's cleared already
    }
    
    private void ClearEventCard()
    {
        _currentEventCard = null;
    }
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
