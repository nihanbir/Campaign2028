using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MainPhaseGameManager : BasePhaseGameManager
{
    private Card _currentTargetCard;     
    private EventCard _currentEventCard;
    private readonly Dictionary<Player, EventCard> _heldEvents = new();
    public List<EventCard> availableEventCards = new();
    public List<Card> shuffledDeck = new();
    
    [HideInInspector] public EventManager eventManager;

    private bool _eventActive;

    public MainPhaseGameManager(GameManager gm) : base(gm) { }

    public override void InitializePhase()
    {
        Debug.Log("=== MAIN PHASE START ===");
        eventManager = new EventManager(this);

        ShuffleDecks();
        game.currentPlayerIndex = 0;
        StartPlayerTurn();
    }

    private void ShuffleDecks()
    {
        shuffledDeck.AddRange(game.stateDeck);
        shuffledDeck.AddRange(game.institutionDeck);
        shuffledDeck = shuffledDeck.OrderBy(_ => Random.value).ToList();
        
        Debug.Log("shuffling");
        availableEventCards.AddRange(game.eventDeck.OrderBy(_ => Random.value).ToList());
    }
    
    public override void StartPlayerTurn()
    {
        Player current = game.CurrentPlayer;
        Debug.Log($"--- Player {current.playerID} turn started (Main Phase) ---");
        
        // // Step 1: Handle target card
        if (_currentTargetCard == null)
        {
            _currentTargetCard = DrawTargetCard();
        }

        // Step 2: Handle event card
        _currentEventCard = DrawEventCard();

        if (_currentEventCard == null)
        {
            Debug.Log("End of event cards");
            return;
        } 

        GameUIManager.Instance.mainUI.OnPlayerTurnStarted(current);
        
        if (AIManager.Instance.IsAIPlayer(current))
        {
            var aiPlayer = AIManager.Instance.GetAIPlayer(current);
            game.StartCoroutine(AIManager.Instance.mainAI.ExecuteAITurn(aiPlayer, _currentEventCard));
        }
    }

    public override void EndPlayerTurn()
    {
        game.CurrentPlayer.ResetPlayerRollCount();
        
        GameUIManager.Instance.mainUI.OnPlayerTurnEnded(game.CurrentPlayer);
        
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
            Debug.Log("Player already rolled");
            return;
        }
        
        int roll = GameUIManager.Instance.DiceRoll;
        
        Debug.Log($"Player {current.playerID} rolled {roll}");
        EvaluateCapture(current, roll);
    }

    private void EvaluateCapture(Player player, int roll)
    {
        bool success = _currentTargetCard switch
        {
            StateCard state => state.IsSuccessfulRoll(roll, player.assignedActor.team),
            InstitutionCard inst => inst.IsSuccessfulRoll(roll, player.assignedActor.team),
            _ => false
        };

        if (success)
        {
            CaptureCard(player, _currentTargetCard);
        }
        else
        {
            Debug.Log($"Player {player.playerID} failed to capture {_currentTargetCard.cardName}");
        }
        
        EndPlayerTurn();
    }

    private void CaptureCard(Player player, Card card)
    {
        if (card == null) return;
        
        player.CaptureCard(card);
        
        GameUIManager.Instance.mainUI.OnCardCaptured(player, card);
        _currentTargetCard = null; // will draw a new one next round
    }

    private Card DrawTargetCard()
    {
        Card card = shuffledDeck[0];
        Debug.Log("Drawing target card");
    
        if (card == null)
        {
            Debug.LogWarning("No target card drawn — decks empty!");
            return null;
        }
    
        shuffledDeck.RemoveAt(0);
        
        GameUIManager.Instance.mainUI.SpawnTargetCard(card);
    
        return card;
    }

    private EventCard DrawEventCard()
    {
        if (availableEventCards.Count == 0)
            return null;

        Debug.Log("Draw Event Card");
        var card = availableEventCards[0];
        availableEventCards.RemoveAt(0);
        
        GameUIManager.Instance.mainUI.SpawnEventCard(card);
        
        return card;
    }

    public bool TrySaveEvent(EventCard card)
    {
        if (_heldEvents.TryAdd(game.CurrentPlayer, card))
        {
            game.CurrentPlayer.heldEvent = card;
            
            Debug.Log($"Player {game.CurrentPlayer.playerID} saved event {card.cardName}");
            GameUIManager.Instance.mainUI.OnCardSaved();
            return true;
        }

        Debug.Log("Player already has a saved event");
        return false;
    }
}

