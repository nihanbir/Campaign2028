using System;
using System.Collections.Generic;
using DG.Tweening;
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
    
    [Header("Buttons")]
    [SerializeField] private Button playEventButton;
    [SerializeField] private Button saveEventButton;
    [SerializeField] private Button drawEventButton;
    [SerializeField] private Button drawTargetButton;

    [Header("PlayerPanel")] 
    [SerializeField] private Button playerPanelButton;
    [SerializeField] private OwnedCardsPanel ownedCardsPanel;
    
    [Header("Event UIs")]
    [SerializeField] public EUM_ChallengeEvent challengeEvent;
    
    [Header("UI Animation Settings")]
    public float cardSpawnDuration = 0.4f;
    
    private GameObject _currentTargetGO;
    private GameObject _currentEventGO;
    private EventDisplayCard _currentEventDisplayCard;

    private GM_MainPhase _mainPhase;
    private EventManager _eventManager;
    private PlayerDisplayCard _currentPlayerDisplayCard;
    
    private Action<StateCard> _stateDiscardedHandler;
    
    protected override void OnPhaseEnabled()
    {
        _mainPhase = game.mainPhase;
        
        ClearTargetCard();
        ClearEventCard();
        
        SetEventButtonsInteractable(false);
        
        if (_mainPhase == null)
        {
            Debug.LogError("MainPhaseGameManager not found. Ensure it's initialized before UI.");
            return;
        }
        
        _eventManager = _mainPhase.EventManager;
        
        //TODO: maybe SOLID
        challengeEvent.Initialize();
        RelocatePlayerCards(playerUIParent);
        
        //TODO: dont forget to remove
        // InitializePlayersForTesting();
        
        base.OnPhaseEnabled();
    }

    protected override void SubscribeToPhaseEvents()
    {
        base.SubscribeToPhaseEvents();
        
        playerPanelButton.onClick.AddListener(TogglePlayerPanel);
        drawEventButton.onClick.AddListener(OnSpawnEventClicked);
        drawTargetButton.onClick.AddListener(OnSpawnTargetClicked);
        
        saveEventButton.onClick.AddListener(OnClickEventSave);
        playEventButton.onClick.AddListener(OnClickEventApply);
        
        _mainPhase.OnPlayerTurnStarted += OnPlayerTurnStarted;
        _mainPhase.OnPlayerTurnEnded += OnPlayerTurnEnded;
        _mainPhase.OnCardCaptured += OnCardCaptured;
        
        _stateDiscardedHandler = _ => ClearTargetCard();
        _mainPhase.OnStateDiscarded += _stateDiscardedHandler;
        
    }

    protected override void UnsubscribeToPhaseEvents()
    {
        base.UnsubscribeToPhaseEvents();
        
        if (_mainPhase == null) _mainPhase = game.mainPhase;
        if (_eventManager == null) _eventManager = _mainPhase.EventManager;
        
        playerPanelButton.onClick.RemoveAllListeners();
        drawEventButton.onClick.RemoveAllListeners();
        drawTargetButton.onClick.RemoveAllListeners();
        
        saveEventButton.onClick.RemoveAllListeners();
        playEventButton.onClick.RemoveAllListeners();
        
        _mainPhase.OnPlayerTurnStarted -= OnPlayerTurnStarted;
        _mainPhase.OnPlayerTurnEnded -= OnPlayerTurnEnded;
        _mainPhase.OnCardCaptured -= OnCardCaptured;
        
        if (_stateDiscardedHandler != null)
            _mainPhase.OnStateDiscarded -= _stateDiscardedHandler;
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

    protected override void OnPlayerTurnStarted(Player player)
    {
        base.OnPlayerTurnStarted(player);

        _currentPlayerDisplayCard = player.PlayerDisplayCard;
        
        EnableDiceButton(false);
        
        if (!isPlayerAI)
        {
            
            if (!_currentTargetGO)
                drawTargetButton.interactable = true;
            
            drawEventButton.interactable = true;
        }
    }

    protected override void OnPlayerTurnEnded(Player player)
    {
       base.OnPlayerTurnEnded(player);
       
       player.PlayerDisplayCard.ShowDice(false);
    }

    public override void OnRollDiceClicked()
    {
        base.OnRollDiceClicked();

        int roll = GameUIManager.Instance.DiceRoll;
        
        UpdateRollButtonState();
        
        _mainPhase.PlayerRolledDice(roll);
        
    }

#endregion Turn Flow
    
#region Card Spawning

    public void OnSpawnTargetClicked()
    {
        drawTargetButton.interactable = false;
        
        if (_currentTargetGO) Destroy(_currentTargetGO);
        
        var card = _mainPhase.DrawTargetCard();
        
        if (card == null) return;
        
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
        
        AnimateCardSpawn(_currentTargetGO.transform, 0.1f);
    }

    public void OnSpawnEventClicked()
    {
        drawEventButton.interactable = false;
        
        if (_currentEventGO) Destroy(_currentEventGO);
        
        EventCard card = _mainPhase.DrawEventCard();
        
        _currentEventGO = Instantiate(eventCardPrefab, eventArea);
        _currentEventDisplayCard = _currentEventGO.GetComponent<EventDisplayCard>();
        _currentEventDisplayCard?.SetCard(card);
        
        AnimateCardSpawn(_currentEventGO.transform, 0.1f);

        SetEventButtonsInteractable(!isPlayerAI);
    }
    
    private void SetEventButtonsInteractable(bool interactable)
    {
        playEventButton.interactable = interactable;

        bool canSave = false;
        if (_currentEventDisplayCard && _currentEventDisplayCard.GetCard() != null)
            canSave = _currentEventDisplayCard.GetCard().canSave && interactable;

        saveEventButton.interactable = canSave;
        
    }

#endregion Card Spawning
    
#region Card Feedback

    private void OnCardCaptured(Player player, Card card)
    {
        AnimateCardCaptured(_currentTargetGO, player.PlayerDisplayCard.transform, out var anim);
        
        anim.OnComplete(() =>
        {
            _mainPhase.CaptureCard(player, card);
            ClearTargetCard();
            _mainPhase.EndPlayerTurn();
        });
        
    }

    private void ClearEventCard()
    {
        if (!_currentEventGO) return;
        
        SetEventButtonsInteractable(false);
        
        _currentEventDisplayCard = null;
        
        Destroy(_currentEventGO);
        _currentEventGO = null;
    }
    
    private void ClearTargetCard()
    {
        if (_currentTargetGO)
        {
            Destroy(_currentTargetGO);
            _currentTargetGO = null;
        }
    }

    public void OnClickEventApply()
    {
        AnimateEventApplied(_currentEventGO, out var anim);

        if (anim != null)
        {
            anim.OnComplete(() =>
            {
                _eventManager.ApplyEvent(GameManager.Instance.CurrentPlayer, _currentEventDisplayCard.GetCard());
        
                ClearEventCard();

                if (!isPlayerAI)
                    UpdateRollButtonState();
            });
        }
        
    }

    public void OnClickEventSave()
    {
        if (_mainPhase.TrySaveEvent(_currentEventDisplayCard.GetCard()))
        {
            AnimateEventSaved(_currentEventGO, _currentPlayerDisplayCard.transform, out var saveAnim);
            if (saveAnim != null)
            {
                saveAnim.OnComplete(() =>
                {
                    ClearEventCard();
                    if (!isPlayerAI)
                        UpdateRollButtonState();
                });
            }
        }
    }
    
#endregion Card Feedback

    #region Animations

    private void AnimateCardSpawn(Transform card, float delay)
    {
        card.localScale = Vector3.zero;
        card.DOScale(1f, cardSpawnDuration)
            .SetEase(Ease.OutBack)
            .SetDelay(delay)
            .SetUpdate(true);
    }
    
    private void AnimateEventApplied(GameObject eventGO, out Sequence seq)
    {
        seq = null;
        if (!eventGO) return;

        var t = eventGO.transform;
        t.DOKill();
        
        seq = DOTween.Sequence();
        seq.Append(t.DOShakePosition(0.3f, 15f, 10, 90, false, true));
        seq.Join(t.DOScale(1.2f, 0.15f).SetLoops(2, LoopType.Yoyo));
        seq.Append(t.DOScale(0f, 0.25f).SetEase(Ease.InBack));
    }

    private void AnimateCardCaptured(GameObject cardGO, Transform target, out Sequence seq)
    {
        seq = null;
        if (!cardGO) return;

        var t = cardGO.transform;
        t.DOKill();

        seq = DOTween.Sequence();
        Vector3 targetPos = target.position;
        seq.Append(t.DOMove(targetPos, 0.5f).SetEase(Ease.InBack));
        seq.Join(t.DOScale(1.3f, 0.5f * 0.5f).SetLoops(2, LoopType.Yoyo));
    }

    private void AnimateEventSaved(GameObject eventGO, Transform target, out Sequence seq)
    {
        seq = null;
        if (!eventGO) return;

        var t = eventGO.transform;
        t.DOKill();

        seq = DOTween.Sequence();
        
        Vector3 targetPos = target.position;
        seq.Append(t.DOMove(targetPos, 0.5f).SetEase(Ease.InBack));
        seq.Join(t.DOScale(1.3f, 0.5f * 0.5f).SetLoops(2, LoopType.Yoyo));
    }
    
    #endregion

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

    private void UpdateRollButtonState()
    {
        var currentPlayer = GameManager.Instance.CurrentPlayer;

        // 🔒 Disable by default
        bool enable = false;

        // Conditions:
        // - Not AI
        // - Not while an event is active
        // - Player has either played or saved their event
        if (!isPlayerAI)
        {
            var eventManager = _eventManager ?? _mainPhase.EventManager;

            bool eventInactive = !eventManager.IsEventActive;
            bool playerHasResolvedEvent = _currentEventGO == null;
            bool canRoll = currentPlayer.CanRoll();

            enable = eventInactive && playerHasResolvedEvent && canRoll;
        }

        Debug.Log($"{enable}");
        EnableDiceButton(enable);
    }
}
