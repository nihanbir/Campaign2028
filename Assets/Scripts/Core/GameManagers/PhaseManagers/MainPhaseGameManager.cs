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

    public MainPhaseGameManager(GameManager gm) : base(gm) { }

    public override void InitializePhase()
    {
        Debug.Log("=== MAIN PHASE START ===");
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
        int roll = GameUIManager.Instance.DiceRoll;
        Player current = game.CurrentPlayer;

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
        card.isCaptured = true;
        
        player.UpdatePlayerScore(card);
    
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

    
    //Called from display card buttons
    public void ApplyEventEffect(Player player, EventCard card)
    {
        Debug.Log($"Applying event {card.cardName} for Player {player.playerID}");
        // TODO: Implement event effects (ExtraRoll, LoseTurn, etc.)
    }

    public bool TrySaveEvent(Player player, EventCard card)
    {
        if (_heldEvents.TryAdd(player, card))
        {
            Debug.Log($"Player {player.playerID} saved event {card.cardName}");
            return true;
        }

        Debug.Log("Player already has a saved event");
        return false;
    }
}

