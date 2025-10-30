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

    // === Events for UI or external systems ===
    public event Action<Player, Card> OnCardCaptured;
    public event Action<Player> OnPlayerTurnStarted;
    public event Action<Player> OnPlayerTurnEnded;
    public event Action<Card> OnCardReturnedToDeck;
    public event Action<EventCard> OnCardSaved;

    public MainPhaseGameManager(GameManager gm) : base(gm) { }

    public override void InitializePhase()
    {
        Debug.Log("=== MAIN PHASE START ===");
        EventManager = new EventManager(this);

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

        _currentTargetCard ??= DrawTargetCard();
        
        _currentEventCard ??= DrawEventCard();

        OnPlayerTurnStarted?.Invoke(current);

        if (AIManager.Instance.IsAIPlayer(current))
        {
            var aiPlayer = AIManager.Instance.GetAIPlayer(current);
            game.StartCoroutine(AIManager.Instance.mainAI.ExecuteAITurn(aiPlayer, _currentEventCard));
        }
    }

    public override void EndPlayerTurn()
    {
        Player current = game.CurrentPlayer;
        current.ResetRollCount();
        OnPlayerTurnEnded?.Invoke(current);

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

        EvaluateCapture(current, roll);
    }

    private void EvaluateCapture(Player player, int roll)
    {
        bool success = _currentTargetCard switch
        {
            StateCard s => s.IsSuccessfulRoll(roll, player.assignedActor.team),
            InstitutionCard i => i.IsSuccessfulRoll(roll, player.assignedActor.team),
            _ => false
        };

        if (success)
        {
            player.CaptureCard(_currentTargetCard);
            OnCardCaptured?.Invoke(player, _currentTargetCard);
            _currentTargetCard = null;
            EndPlayerTurn();
        }
        else if (player.CanRoll())
        {
            Debug.Log($"Player {player.playerID} failed to capture {_currentTargetCard.cardName}");
            StartPlayerTurn();
        }
        else
        {
            EndPlayerTurn();
        }
    }

    private Card DrawTargetCard()
    {
        if (_mainDeck.Count == 0)
        {
            Debug.LogWarning("Main deck empty!");
            return null;
        }

        Card drawn = _mainDeck.PopFront();
        GameUIManager.Instance.mainUI.SpawnTargetCard(drawn);
        return drawn;
    }

    private EventCard DrawEventCard()
    {
        if (_eventDeck.Count == 0) return null;

        EventCard card = _eventDeck.PopFront();
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
        OnCardSaved?.Invoke(card);

        _currentEventCard = null;
        return true;
    }

    public void ReturnCardToDeck(Card card)
    {
        if (card is not EventCard eventCard) return;

        _eventDeck.Insert(UnityEngine.Random.Range(0, _eventDeck.Count + 1), eventCard);
        OnCardReturnedToDeck?.Invoke(card);
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
