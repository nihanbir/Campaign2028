using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainPhaseUIManager : MonoBehaviour
{
    [Header("Main Phase Elements")]     
    [SerializeField] private GameObject mainGamePhase;
    [SerializeField] private Button rollDiceButton;

    [Header("Cards")]
    [SerializeField] private GameObject stateCardPrefab;
    [SerializeField] private GameObject institutionCardPrefab;
    [SerializeField] private GameObject eventCardPrefab;
    [SerializeField] private Transform tableArea;
    [SerializeField] private Transform eventArea;

    [Header("Players")]
    [SerializeField] private Transform playerUIParent;
    [SerializeField] private float spacingBetweenPlayerCards = 150f;

    private bool _isPlayerAI = false;
    
    private GameObject _currentTargetGO;
    private GameObject _currentEventGO;
    private EventDisplayCard _currentEventDisplayCard;

    private MainPhaseGameManager _mainPhase;
    private EventManager _eventManager;
    
    public void InitializePhaseUI()
    {
        _mainPhase = GameManager.Instance?.mainPhase;

        if (_mainPhase == null)
        {
            Debug.LogError("MainPhaseGameManager not found. Ensure it's initialized before UI.");
            return;
        }
        
        _eventManager = _mainPhase.EventManager;

        SubscribeToPhaseEvents();

        if (rollDiceButton)
            rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        
        EnableDiceButton(false);
        
        // RelocatePlayerCards(playerUIParent, spacingBetweenPlayerCards);
        InitializePlayersForTesting();
    }
    
    private void SubscribeToPhaseEvents()
    {
        _mainPhase.OnPlayerTurnStarted += OnPlayerTurnStarted;
        _mainPhase.OnPlayerTurnEnded += OnPlayerTurnEnded;
        _mainPhase.OnCardCaptured += OnCardCaptured;
        _mainPhase.OnCardSaved += _ => OnEventSaved();
        _eventManager.OnEventApplied += _ => OnEventApplied();
        
    }

    private void OnDestroy()
    {
        if (_mainPhase == null) return;

        _mainPhase.OnPlayerTurnStarted -= OnPlayerTurnStarted;
        _mainPhase.OnPlayerTurnEnded -= OnPlayerTurnEnded;
        _mainPhase.OnCardCaptured -= OnCardCaptured;
        _mainPhase.OnCardSaved -= _ => ClearEventCard();
        _eventManager.OnEventApplied -= _ => OnEventApplied();
        
    }

#region Player Management
    private void RelocatePlayerCards(Transform parent, float spacing)
    {
        var players = GameManager.Instance?.players;
        if (players == null || players.Count == 0)
        {
            Debug.LogError("No players found for relocation.");
            return;
        }

        float totalWidth = (players.Count - 1) * spacing;

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            var displayCard = player.PlayerDisplayCard;

            if (displayCard == null)
            {
                Debug.LogWarning($"Player {player.playerID} has no display card assigned.");
                continue;
            }

            displayCard.transform.SetParent(parent, false);

            if (displayCard.TryGetComponent(out RectTransform rt))
            {
                float xPos = i * spacing - totalWidth / 2f;
                rt.anchoredPosition = new Vector2(xPos, 0);
            }

            displayCard.displayType = CardDisplayType.AssignedActor;
            displayCard.gameObject.SetActive(true);
        }
    }

#endregion Player Management

#region Turn Flow
    private void OnPlayerTurnStarted(Player player)
    {
        _isPlayerAI = AIManager.Instance.IsAIPlayer(player);

        player.PlayerDisplayCard.Highlight();
    }

    private void OnPlayerTurnEnded(Player player)
    {
        EnableDiceButton(false);
        player.PlayerDisplayCard.ShowDice(false);
        player.PlayerDisplayCard.RemoveHighlight();
        ClearEventCard();
    }

    private void EnableDiceButton(bool enable)
    {
        if (!rollDiceButton) return;
        if (_isPlayerAI)
        {
            enable = false;
        }
        rollDiceButton.interactable = enable;
    }

    public void OnRollDiceClicked()
    {
        var currentPlayer = GameManager.Instance.CurrentPlayer;
        
        GameUIManager.Instance.OnRollDiceClicked(rollDiceButton);
        currentPlayer.PlayerDisplayCard.SetRolledDiceImage();

        EnableDiceButton(currentPlayer.CanRoll());
        _mainPhase.PlayerRolledDice();
        
        //TODO:Do this when extra roll added
        
    }

#endregion Turn Flow
    
#region Card Spawning
    public void SpawnTargetCard(Card card)
    {
        if (_currentTargetGO) Destroy(_currentTargetGO);

        GameObject prefab = card switch
        {
            InstitutionCard => institutionCardPrefab,
            StateCard => stateCardPrefab,
            _ => null
        };

        if (prefab == null)
        {
            Debug.LogWarning($"Unsupported card type: {card.GetType().Name}");
            return;
        }

        _currentTargetGO = Instantiate(prefab, tableArea);
        if (_currentTargetGO.TryGetComponent(out IDisplayCard display))
        {
            display.SetCardBase(card);
        }
        else
        {
            Debug.LogError($"{prefab.name} missing IDisplayCard component.");
        }
    }

    public void SpawnEventCard(EventCard card)
    {
        if (_currentEventGO) Destroy(_currentEventGO);

        _currentEventGO = Instantiate(eventCardPrefab, eventArea);
        _currentEventDisplayCard = _currentEventGO.GetComponent<EventDisplayCard>();
        _currentEventDisplayCard?.SetCard(card);
        
        if (_currentEventDisplayCard)
            _currentEventDisplayCard.SetButtonsVisible(!_isPlayerAI);
    }

#endregion Card Spawning
    
#region Card Feedback

    private void OnCardCaptured(Player player, Card card)
    {
        player.PlayerDisplayCard.UpdateScore();
        if (_currentTargetGO) Destroy(_currentTargetGO);
    }

    private void ClearEventCard()
    {
        if (!_currentEventGO) return;
        
        _currentEventDisplayCard = null;
        
        Destroy(_currentEventGO);
    }

    private void OnEventApplied()
    {
        ClearEventCard();
        
        if (GameManager.Instance.CurrentPlayer.CanRoll())
        {
            EnableDiceButton(true);
        }
    }

    private void OnEventSaved()
    {
        ClearEventCard();
     
        //TODO: UI work
        
        if (GameManager.Instance.CurrentPlayer.CanRoll())
        {
            EnableDiceButton(true);
        }
    }
    
#endregion Card Feedback

#region Testing Helper

    public void InitializePlayersForTesting()
    {
        var gm = GameManager.Instance;
        var ui = GameUIManager.Instance;

        if (gm == null || gm.players.Count == 0)
        {
            Debug.LogError("GameManager or players missing!");
            return;
        }

        var allActors = new List<ActorCard>(gm.actorDeck);
        if (allActors.Count == 0)
        {
            Debug.LogError("No actor cards found in GameManager!");
            return;
        }

        for (int i = 0; i < gm.players.Count; i++)
        {
            var player = gm.players[i];
            var actor = allActors[i % allActors.Count];

            player.assignedActor = actor;

            // Spawn player display card for this actor
            var displayGo = Instantiate(ui.setupUI.cardDisplayPrefab, playerUIParent);
            var displayCard = displayGo.GetComponent<PlayerDisplayCard>();

            if (displayCard != null)
            {
                displayCard.SetCard(actor);
                displayCard.displayType = CardDisplayType.AssignedActor;
                player.SetDisplayCard(displayCard);
            }
        }

        RelocatePlayerCards(playerUIParent, spacingBetweenPlayerCards);
        Debug.Log("[MainPhaseUIManager] Test players initialized successfully!");
    }
#endregion Testing Helper
    
}
