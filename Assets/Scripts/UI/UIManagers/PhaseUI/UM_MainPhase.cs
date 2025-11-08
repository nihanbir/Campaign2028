using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UM_MainPhase : UM_BasePhase
{
    public override GamePhase PhaseType => GamePhase.MainGame;
    
    [Header("Cards")]
    [SerializeField] public GameObject stateCardPrefab;
    [SerializeField] public GameObject institutionCardPrefab;
    [SerializeField] private GameObject eventCardPrefab;
    [SerializeField] private Transform tableArea;
    [SerializeField] private Transform eventArea;

    [Header("Players")]
    [SerializeField] private Transform playerUIParent;

    [Header("PlayerPanel")] 
    [SerializeField] private Button playerPanelButton;
    [SerializeField] private OwnedCardsPanel ownedCardsPanel;
    
    [Header("Event UI")]     
    [SerializeField] public ChallengeStateUIManager challengeUI;
    [SerializeField] public AlternativeStatesUIManager altStateUI;
    
    private GameObject _currentTargetGO;
    private GameObject _currentEventGO;
    private EventDisplayCard _currentEventDisplayCard;

    private GM_MainPhase _mainPhase;
    private EventManager _eventManager;
    
    protected override void OnPhaseEnabled()
    {
        _mainPhase = game.mainPhase;
        
        if (_mainPhase == null)
        {
            Debug.LogError("MainPhaseGameManager not found. Ensure it's initialized before UI.");
            return;
        }
        
        _eventManager = _mainPhase.EventManager;
        
        challengeUI.InitializeEventUI();
        altStateUI.InitializeEventUI();
        
        // RelocatePlayerCards(playerUIParent, spacingBetweenPlayerCards);
        
        //TODO: dont forget to remove
        InitializePlayersForTesting();
        
        base.OnPhaseEnabled();
    }

    protected override void SubscribeToPhaseEvents()
    {
        base.SubscribeToPhaseEvents();
        
        playerPanelButton.onClick.AddListener(TogglePlayerPanel);
        _mainPhase.OnPlayerTurnStarted += OnPlayerTurnStarted;
        _mainPhase.OnPlayerTurnEnded += OnPlayerTurnEnded;
        _mainPhase.OnCardCaptured += OnCardCaptured;
        _mainPhase.OnCardSaved += _ => OnEventSaved();
        _mainPhase.OnStateDiscarded += _ => ClearTargetCard();
        _eventManager.OnEventApplied += _ => OnEventApplied();
        
    }

    protected override void UnsubscribeToPhaseEvents()
    {
        base.UnsubscribeToPhaseEvents();
        
        if (_mainPhase == null) _mainPhase = game.mainPhase;
        if (_eventManager == null) _eventManager = _mainPhase.EventManager;
        
        playerPanelButton.onClick.RemoveAllListeners();
        _mainPhase.OnPlayerTurnStarted -= OnPlayerTurnStarted;
        _mainPhase.OnPlayerTurnEnded -= OnPlayerTurnEnded;
        _mainPhase.OnCardCaptured -= OnCardCaptured;
        _mainPhase.OnCardSaved -= _ => OnEventSaved();
        _mainPhase.OnStateDiscarded -= _ => ClearTargetCard();
        _eventManager.OnEventApplied -= _ => OnEventApplied();
    }
    
    private void TogglePlayerPanel()
    {
        ownedCardsPanel.TogglePanel();
    }

#region Player Management
    private void RelocatePlayerCards(Transform parent)
    {
        var players = GameManager.Instance?.players;
        if (players == null || players.Count == 0)
        {
            Debug.LogError("No players found for relocation.");
            return;
        }

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

            displayCard.displayType = CardDisplayType.AssignedActor;
            displayCard.gameObject.SetActive(true);
        }
    }

#endregion Player Management

#region Turn Flow
    
    protected override void OnPlayerTurnEnded(Player player)
    {
       base.OnPlayerTurnEnded(player);
       player.PlayerDisplayCard.ShowDice(false);
       ClearEventCard();
    }

    public override void OnRollDiceClicked()
    {
        base.OnRollDiceClicked();

        int roll = GameUIManager.Instance.DiceRoll;
        _mainPhase.PlayerRolledDice(roll);
        
        EnableDiceButton(GameManager.Instance.CurrentPlayer.CanRoll());
        
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
            display.SetCard(card);
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
            _currentEventDisplayCard.SetButtonsVisible(!isPlayerAI);
    }

#endregion Card Spawning
    
#region Card Feedback

    private void OnCardCaptured(Player player, Card card)
    {
        if (_currentTargetGO) Destroy(_currentTargetGO);
    }

    private void ClearEventCard()
    {
        if (!_currentEventGO) return;
        
        _currentEventDisplayCard = null;
        
        Destroy(_currentEventGO);
    }
    
    private void ClearTargetCard()
    {
        if (_currentTargetGO) Destroy(_currentTargetGO);
    }

    private void OnEventApplied()
    {
        ClearEventCard();
        
        if (!_eventManager.IsEventActive && GameManager.Instance.CurrentPlayer.CanRoll())
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

        // Remove these
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

        // RelocatePlayerCards(playerUIParent);
        Debug.Log("[MainPhaseUIManager] Test players initialized successfully!");
    }
#endregion Testing Helper
    
}
