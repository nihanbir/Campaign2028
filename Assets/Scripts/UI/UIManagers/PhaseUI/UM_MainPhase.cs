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
    
    private TargetDisplayCard _currentTargetDisplayCard;
    private EventDisplayCard _currentEventDisplayCard;
    private PlayerDisplayCard _currentPlayerDisplayCard;

    private GM_MainPhase _mainPhase;
    private EventManager _eventManager;
    
    protected override void OnPhaseEnabled()
    {
        _mainPhase = game.mainPhase;
        
        ClearCurrentEventCard();
        ClearCurrentTargetCard();
        
        UpdateRollButtonState();
        SetEventButtonsInteractable(false);
        
        if (_mainPhase == null)
        {
            Debug.LogError("MainPhaseGameManager not found. Ensure it's initialized before UI.");
            return;
        }
        
        _eventManager = _mainPhase.EventManager;
        
        challengeEvent.Initialize();
        
        // RelocatePlayerCards(playerUIParent);
        
        //TODO: dont forget to remove
        InitializePlayersForTesting();
        
        base.OnPhaseEnabled();
    }

    protected override void HandleTurnEvent(IGameEvent e)
    {
        base.HandleTurnEvent(e);

        if (!isActive) return;
        
        if (e is MainStageEvent m)
        {
            switch (m.stage)
            {
                case MainStage.EventCardDrawn:
                    SpawnEventCard((EventCard)m.payload);
                    break;
                
                case MainStage.TargetCardDrawn:
                    SpawnTargetCard((Card)m.payload);
                    break;
                
                case MainStage.StateDiscarded:
                    ClearCurrentTargetCard();
                    break;
                
                case MainStage.EventCardSaved:
                    EventSaved((EventCard)m.payload);
                    break;
                
                case MainStage.CardCaptured:
                    var captured = (CardCapturedData)m.payload;
                    CardCaptured(captured.player, captured.card);
                    break;
            }
        }
    }

    protected override void SubscribeToPhaseEvents()
    {
        base.SubscribeToPhaseEvents();
        
        playerPanelButton.onClick.AddListener(TogglePlayerPanel);
        drawEventButton.onClick.AddListener(OnSpawnEventClicked);
        drawTargetButton.onClick.AddListener(OnSpawnTargetClicked);
        
        saveEventButton.onClick.AddListener(OnClickEventSave);
        playEventButton.onClick.AddListener(OnClickEventApply);
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
        
        if (!isPlayerAI)
        {
            if (_currentTargetDisplayCard.IsNull())
                drawTargetButton.interactable = true;
            
            drawEventButton.interactable = true;
        }
    }

    protected override void OnPlayerTurnEnded(Player player)
    {
       base.OnPlayerTurnEnded(player);

       _currentPlayerDisplayCard = null;
       
       player.PlayerDisplayCard.ShowDice(false);
    }

    
    protected override void OnPlayerRolledDice(Player player, int roll)
    {
        base.OnPlayerRolledDice(player, roll);
        
        UpdateRollButtonState();
    }

    #endregion Turn Flow
    
    #region Card Spawning
    
    //TODO:
    public void OnSpawnTargetClicked()
    {
        TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.DrawTargetCardRequest));
    }

    private void SpawnTargetCard(Card card)
    {
        if (card == null) return;
        
        drawTargetButton.interactable = false;
        
        ClearCurrentTargetCard();
        
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
        
        GameObject go = Instantiate(prefab, tableArea);

        if (card is StateCard state)
        {
            _currentTargetDisplayCard.state = go.GetComponent<StateDisplayCard>();
            _currentTargetDisplayCard.state.SetCard(state);
        }
        else if (card is InstitutionCard inst)
        {
            _currentTargetDisplayCard.inst = go.GetComponent<InstitutionDisplayCard>();
            _currentTargetDisplayCard.inst.SetCard(inst);
        }
        
        //TODO: Enqueue
        AnimateCardSpawn(go.transform, 0.1f);
    }

    private void OnSpawnEventClicked()
    {
        TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.DrawEventCardRequest));
    }
    
    private void SpawnEventCard(EventCard card)
    {
        drawEventButton.interactable = false;
        
        var eventDisplay = Instantiate(eventCardPrefab, eventArea);
        _currentEventDisplayCard = eventDisplay.GetComponent<EventDisplayCard>();
        _currentEventDisplayCard?.SetCard(card);
        
        //TODO: Enqueue
        AnimateCardSpawn(eventDisplay.transform, 0.1f);

        SetEventButtonsInteractable(true);
    }
    
    private void SetEventButtonsInteractable(bool interactable)
    {
        bool canSave = false;
        
        if (_currentEventDisplayCard == null)
        {
            interactable = false;
        }
        else
        {
            canSave = _currentEventDisplayCard.GetCard().canSave && interactable;
        }

        saveEventButton.interactable = canSave;
        
        playEventButton.interactable = interactable;
    }

    #endregion Card Spawning
    
    #region Card Feedback

    private void CardCaptured(Player player, Card card)
    {
        if (!_currentTargetDisplayCard.IsTarget(card))
        {
            Debug.LogWarning($"Tried to animate {card.cardName} capture but current displayed target was {_currentTargetDisplayCard}");
            return;
        }
        
        AnimateCardCaptured(_currentTargetDisplayCard.Transform, player.PlayerDisplayCard.transform, out var anim);
        
        anim.OnComplete(ClearCurrentTargetCard);
    }

    private void ClearCurrentEventCard()
    {
        if (_currentEventDisplayCard && _currentEventDisplayCard.gameObject)
        {
            Destroy(_currentEventDisplayCard.gameObject);
            _currentEventDisplayCard = null;
        }
        
        SetEventButtonsInteractable(false);
    }
    
    private void ClearCurrentTargetCard()
    {
        _currentTargetDisplayCard.Clear();
        
        SetEventButtonsInteractable(false);
    }
    
    private void OnClickEventApply()
    {
        TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.ApplyEventCardRequest, _currentEventDisplayCard.GetCard()));
    }

    private void EventApplied()
    {
        AnimateEventApplied(_currentEventDisplayCard.gameObject, out var anim);

        if (anim != null)
        {
            anim.OnComplete(() =>
            {
                //TODO:
                _eventManager.ApplyEvent(GameManager.Instance.CurrentPlayer, _currentEventDisplayCard.GetCard());
        
                ClearCurrentEventCard();
                
                UpdateRollButtonState();
            });
        }
    }

    private void OnClickEventSave()
    {
        TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.SaveEventCardRequest , _currentEventDisplayCard.GetCard()));
    }

    private void EventSaved(EventCard card)
    {
        if (_currentEventDisplayCard.GetCard() != card)
        {
            Debug.LogWarning($"Tried to animate {card.cardName} save but current displayed event was {_currentEventDisplayCard}");
            return;
        }
        
        AnimateEventSaved(_currentEventDisplayCard.gameObject, _currentPlayerDisplayCard.transform, out var saveAnim);
        if (saveAnim != null)
        {
            saveAnim.OnComplete(() =>
            {
                ClearCurrentEventCard();
                
                UpdateRollButtonState();
            });
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

    private void AnimateCardCaptured(Transform card, Transform target, out Sequence seq)
    {
        seq = null;
        if (!card) return;

        var t = card;
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
            bool playerHasResolvedEvent = _currentEventDisplayCard == null;
            bool targetCardDrawn = !_currentTargetDisplayCard.IsNull();
            
            bool canRoll = currentPlayer.CanRoll();

            enable = eventInactive && playerHasResolvedEvent && canRoll && targetCardDrawn;
        }
        
        EnableDiceButton(enable);
    }
}
