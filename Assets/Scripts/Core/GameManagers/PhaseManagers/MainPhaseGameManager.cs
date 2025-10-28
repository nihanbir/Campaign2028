using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MainPhaseGameManager : BasePhaseGameManager
{
    private Card _currentTargetCard;     
    private EventCard _currentEventCard;
    private readonly Dictionary<Player, List<EventCard>> _heldEvents = new();
    public List<EventCard> availableEventCards = new();

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
        // game.stateDeck = game.stateDeck.OrderBy(_ => Random.value).ToList();
        // game.institutionDeck = game.institutionDeck.OrderBy(_ => Random.value).ToList();
        Debug.Log("shuffling");
        availableEventCards = game.eventDeck.OrderBy(_ => Random.value).ToList();
    }
    
    public override void StartPlayerTurn()
    {
        Player current = game.CurrentPlayer;
        Debug.Log($"--- Player {current.playerID} turn started (Main Phase) ---");

        GameUIManager.Instance.mainUI.OnPlayerTurnStarted(current);

        if (!_heldEvents.ContainsKey(current))
            _heldEvents[current] = new List<EventCard>();

        // // Step 1: Handle target card
        // if (_currentTargetCard == null)
        // {
        //     Debug.Log("currentargetcard is null");
        //     _currentTargetCard = DrawTargetCard();
        //     GameUIManager.Instance.mainUI.SpawnTargetCard(_currentTargetCard);
        // }
        // else
        // {
        //     GameUIManager.Instance.mainUI.ShowExistingTarget(_currentTargetCard);
        // }

        // Step 2: Handle event card
        _currentEventCard = DrawEventCard();
        if (_currentEventCard != null)
        {
            GameUIManager.Instance.mainUI.SpawnEventCard(_currentEventCard);
            HandleEventCard(current, _currentEventCard);
        }

        // Step 3: AI vs Player handling
        if (AIManager.Instance.IsAIPlayer(current))
        {
            AIManager.Instance.mainAI.ExecuteAITurn(AIManager.Instance.GetAIPlayer(current), this);
        }
        else
        {
            GameUIManager.Instance.mainUI.EnableDiceButton(true);
        }
    }

    public override void EndPlayerTurn()
    {
        Player current = game.CurrentPlayer;
        GameUIManager.Instance.mainUI.OnPlayerTurnEnded(current);

        game.currentPlayerIndex = (game.currentPlayerIndex + 1) % game.players.Count;
        StartPlayerTurn();
    }

    public override void MoveToNextPlayer()
    {
        EndPlayerTurn();
    }

    public override void PlayerRolledDice()
    {
        int roll = GameUIManager.Instance.DiceRoll;
        Player current = game.CurrentPlayer;

        Debug.Log($"Player {current.playerID} rolled {roll}");
        // EvaluateCapture(current, roll);
        // EndPlayerTurn();
    }

    // private void EvaluateCapture(Player player, int roll)
    // {
    //     bool success = false;
    //
    //     if (_currentTargetCard is StateCard state)
    //         success = state.IsSuccessfulRoll(roll, player.assignedActor.team);
    //     else if (_currentTargetCard is InstitutionCard inst)
    //         success = inst.IsSuccessfulRoll(roll, player.assignedActor.team);
    //
    //     if (success)
    //     {
    //         CaptureCard(player, _currentTargetCard);
    //     }
    //     else
    //     {
    //         Debug.Log($"Player {player.playerID} failed to capture {_currentTargetCard.cardName}");
    //     }
    // }

    // private void CaptureCard(Player player, Card card)
    // {
    //     if (card == null) return;
    //     card.isCaptured = true;
    //
    //     Debug.Log($"Player {player.playerID} captured {card.cardName}");
    //     GameUIManager.Instance.mainUI.OnCardCaptured(player, card);
    //     _currentTargetCard = null; // will draw a new one next round
    // }

    // -----------------------------------------------------------
    // CARD DRAW LOGIC
    // -----------------------------------------------------------

    // private Card DrawTargetCard()
    // {
    //     bool drawInstitution = Random.value > 0.5f && game.institutionDeck.Count > 0;
    //     Card card = drawInstitution
    //         ? game.institutionDeck[0]
    //         : game.stateDeck.FirstOrDefault();
    //
    //     if (card == null)
    //     {
    //         Debug.LogWarning("No target card drawn — decks empty!");
    //         return null;
    //     }
    //
    //     if (drawInstitution)
    //         game.institutionDeck.RemoveAt(0);
    //     else
    //         game.stateDeck.RemoveAt(0);
    //
    //     return card;
    // }

    private EventCard DrawEventCard()
    {
        if (availableEventCards.Count == 0)
            return null;

        Debug.Log("Draw Event Card");
        var card = availableEventCards[0];
        // availableEventCards.Remove(card);
        availableEventCards.RemoveAt(0);
        return card;
    }

    private void HandleEventCard(Player player, EventCard card)
    {
        if (card == null) return;

        if (card.mustPlayImmediately)
        {
            ApplyEventEffect(player, card);
        }
        else if (card.canSave)
        {
            var saved = _heldEvents[player];
            if (saved.Count == 0)
            {
                saved.Add(card);
                Debug.Log($"Player {player.playerID} saved event {card.cardName}");
            }
            else
            {
                ApplyEventEffect(player, card);
            }
        }
    }

    private void ApplyEventEffect(Player player, EventCard card)
    {
        Debug.Log($"Applying event {card.cardName} for Player {player.playerID}");
        // TODO: Implement event effects (ExtraRoll, LoseTurn, etc.)
    }
}

