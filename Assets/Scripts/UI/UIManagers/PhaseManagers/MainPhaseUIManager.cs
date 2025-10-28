using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainPhaseUIManager : MonoBehaviour
{
    [Header("Setup Phase")]     
    public GameObject mainGamePhase;
    public Button rollDiceButton;
    
    [Header("Cards")]
    // public GameObject stateCardPrefab;
    // public GameObject institutionCardPrefab;
    public GameObject eventCardPrefab;
    // public Transform tableArea;
    public Transform eventArea;
    
    [Header("Players")]
    public Transform playerUIParent;
    public float spacingBetweenPlayerCards = 150f;

    private GameObject _currentTargetGO;
    private GameObject _currentEventGO;
    
    public void InitializePhaseUI()
    {
        if (rollDiceButton)
            rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        
        InitializePlayersForTesting();
        // RelocatePlayerCards(playerUIParent, spacingBetweenPlayerCards);
    }

    void RelocatePlayerCards(Transform parent, float spacing)
    {
        if (!GameManager.Instance)
        {
            Debug.LogError("GameManager instance is not set");
            return;
        }

        var players = GameManager.Instance.players;
        int count = players.Count;
        float totalWidth = (count - 1) * spacing;

        for (int i = 0; i < count; i++)
        {
            var player = players[i];
            var displayCard = player.playerDisplayCard;

            if (displayCard == null)
            {
                Debug.LogWarning($"Player {player.playerID} has no display card assigned from setup phase.");
                continue;
            }

            // Reparent to main phase UI parent
            RectTransform rt = displayCard.GetComponent<RectTransform>();
            displayCard.transform.SetParent(parent, false);

            if (rt)
            {
                float xPos = i * spacing - totalWidth / 2f;
                rt.anchoredPosition = new Vector2(xPos, 0);
            }

            // Ensure it's correctly tagged as assigned actor
            displayCard.displayType = CardDisplayType.AssignedActor;

            // Reactivate if hidden
            displayCard.gameObject.SetActive(true);
        }
    }

    
    public void OnPlayerTurnStarted(Player player)
    {
        EnableDiceButton(!AIManager.Instance.IsAIPlayer(player));
    }

    public void OnPlayerTurnEnded(Player player)
    {
        EnableDiceButton(false);
    }

    public void EnableDiceButton(bool enable)
    {
        if (rollDiceButton)
            rollDiceButton.interactable = enable;
    }

    public void OnRollDiceClicked()
    {
        var currentPlayer = GameManager.Instance.CurrentPlayer;
        
        GameUIManager.Instance.OnRollDiceClicked(rollDiceButton);
        currentPlayer.playerDisplayCard.SetRolledDiceImage();
        
        GameManager.Instance.mainPhase.PlayerRolledDice();
    }

    // public void SpawnTargetCard(Card card)
    // {
    //     if (_currentTargetGO) Destroy(_currentTargetGO);
    //     GameObject prefab = card is InstitutionCard ? institutionCardPrefab : stateCardPrefab;
    //     _currentTargetGO = Instantiate(prefab, tableArea);
    //     _currentTargetGO.GetComponent<Image>().sprite = card.artwork;
    // }

    // public void ShowExistingTarget(Card card)
    // {
    //     if (_currentTargetGO)
    //         _currentTargetGO.GetComponent<Image>().sprite = card.artwork;
    // }

    public void SpawnEventCard(EventCard card)
    {
        if (_currentEventGO) Destroy(_currentEventGO);
        _currentEventGO = Instantiate(eventCardPrefab, eventArea);
        EventDisplayCard displayCard = _currentEventGO.GetComponent<EventDisplayCard>();

        if (displayCard)
        {
            displayCard.SetEventCard(card);
        }
    }

    public void OnCardCaptured(Player player, Card card)
    {
        Debug.Log($"[UI] Player {player.playerID} captured {card.cardName}");
        if (_currentTargetGO) Destroy(_currentTargetGO);
    }
    
    public void InitializePlayersForTesting()
    {
        Debug.Log("[MainPhaseUIManager] Initializing existing players with random actors for testing...");

        var gm = GameManager.Instance;
        var ui = GameUIManager.Instance;
        
        if (gm == null)
        {
            Debug.LogError("GameManager instance not found!");
            return;
        }

        var players = gm.players;
        if (players == null || players.Count == 0)
        {
            Debug.LogError("No players found in GameManager. Cannot initialize testing phase.");
            return;
        }

        var allActors = new List<ActorCard>(gm.actorDeck);
        if (allActors == null || allActors.Count == 0)
        {
            Debug.LogError("No actor cards found in GameManager!");
            return;
        }

        // Assign random actors to players
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            var actor = allActors[i % allActors.Count];

            player.assignedActor = actor;

            // Spawn player display card for this actor
            var displayGo = Instantiate(ui.setupUI.cardDisplayPrefab, playerUIParent);
            var displayCard = displayGo.GetComponent<PlayerDisplayCard>();

            if (displayCard != null)
            {
                displayCard.SetActor(actor);
                displayCard.displayType = CardDisplayType.AssignedActor;
                player.playerDisplayCard = displayCard;
            }
        }

        RelocatePlayerCards(playerUIParent, spacingBetweenPlayerCards);
        Debug.Log("[MainPhaseUIManager] Test players initialized successfully!");
    }

}
