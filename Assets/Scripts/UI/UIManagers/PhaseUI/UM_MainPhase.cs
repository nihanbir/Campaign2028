using System;
using System.Collections;
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
    [SerializeField] public EUM_ChallengeEvent eventUI;
    
    [Header("UI Animation Settings")]
    public float cardSpawnDuration = 0.4f;

    [Header("CanvasGroup")] 
    [SerializeField] private CanvasGroup canvasGroup;
    
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
        
        SetEventButtonsInteractable(false);
        
        if (_mainPhase == null)
        {
            Debug.LogError("MainPhaseGameManager not found. Ensure it's initialized before UI.");
            return;
        }
        
        _eventManager = _mainPhase.EventManager;
        
        eventUI.Initialize();
        
        // RelocatePlayerCards(playerUIParent);
        
        //TODO: dont forget to remove
        InitializePlayersForTesting();
        
        base.OnPhaseEnabled();
    }

    protected override void HandleTurnEvent(IGameEvent e)
    {
        base.HandleTurnEvent(e);

        if (!isCurrent) return;
        
        if (e is MainStageEvent m)
        {
            switch (m.stage)
            {
                case MainStage.EventCardDrawn:
                    EnqueueUI(SpawnEventCard((EventCard)m.payload));
                    break;
                
                case MainStage.TargetCardDrawn:
                    EnqueueUI(SpawnTargetCard((Card)m.payload));
                    break;
                
                // case MainStage.StateDiscarded:
                //     ClearCurrentTargetCard();
                //     break;
                
                case MainStage.EventCardSaved:
                    EnqueueUI(EventSaved((EventCard)m.payload));
                    break;
                
                case MainStage.CardCaptured:
                    var captured = (CardCapturedData)m.payload;
                    EnqueueUI(CardCaptured(captured.player, captured.card));
                    break;
            }
        }

        if (e is EventCardEvent c)
        {
            switch (c.stage)
            {
                case EventStage.EventApplied:
                    EnqueueUI(HandleEventApplied());
                    break;
                
                case EventStage.ChangeToEventScreen:
                    EnqueueUI(HandleChangeToEventScreen());
                    break;
                    
                case EventStage.DuelCompleted:
                    EnqueueUI(HandleChangeFromEventScreen());
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
        
        _mainPhase ??= game.mainPhase;
        _eventManager ??= _mainPhase.EventManager;
        
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

        UpdateRollButtonState();
        EnqueueUI(EnableDrawButtons());
    }

    private IEnumerator EnableDrawButtons()
    {
        if (!isAIPlayer)
        {
            if (_currentTargetDisplayCard.IsNull())
                drawTargetButton.interactable = true;
            
            drawEventButton.interactable = true;
        }
        yield break;
    }

    protected override void OnPlayerTurnEnded(Player player)
    {
       base.OnPlayerTurnEnded(player);

       _currentPlayerDisplayCard = null;
       
       EnqueueUI(HidePlayerDiceImg(player));
    }

    private IEnumerator HidePlayerDiceImg(Player player)
    {
       player.PlayerDisplayCard.ShowDice(false);
        yield break;
    }
    
    protected override void OnPlayerRolledDice(Player player, int roll)
    {
        base.OnPlayerRolledDice(player, roll);
        
        UpdateRollButtonState();
    }

    #endregion Turn Flow
    
    #region Card Spawning
    
    private void OnSpawnTargetClicked()
    {
        TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.DrawTargetCardRequest));
    }

    private IEnumerator SpawnTargetCard(Card card)
    {
        if (card == null) 
            yield break;
        
        drawTargetButton.interactable = false;
        
        GameObject prefab = card switch
        {
            InstitutionCard => institutionCardPrefab,
            StateCard => stateCardPrefab,
            _ => null
        };

        if (prefab == null)
        {
            Debug.LogWarning($"Unsupported card type: {card.GetType().Name}");
            yield break;
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
        
        yield return AnimateCardSpawn(_currentTargetDisplayCard.Transform, 0.1f);
        
        SetEventButtonsInteractable(true);
    }

    private void OnSpawnEventClicked()
    {
        TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.DrawEventCardRequest));
    }

    private IEnumerator SpawnEventCard(EventCard card)
    {
        drawEventButton.interactable = false;
        
        var eventDisplay = Instantiate(eventCardPrefab, eventArea);
        _currentEventDisplayCard = eventDisplay.GetComponent<EventDisplayCard>();
        _currentEventDisplayCard?.SetCard(card);
        
        yield return AnimateCardSpawn(eventDisplay.transform, 0.1f);
        
        SetEventButtonsInteractable(true);
    }
    
    private void SetEventButtonsInteractable(bool interactable)
    {
        bool canSave = false;
        
        if (_currentTargetDisplayCard.IsNull()) 
            interactable = false;
        
        if (_currentEventDisplayCard == null) 
            interactable = false;
        
        else
            canSave = _currentEventDisplayCard.GetCard().canSave && interactable;
        

        saveEventButton.interactable = canSave;
        
        playEventButton.interactable = interactable;
    }

    #endregion Card Spawning
    
    #region Card Feedback

    private IEnumerator CardCaptured(Player player, Card card)
    {
        if (!_currentTargetDisplayCard.IsTarget(card))
        {
            Debug.LogWarning($"Tried to animate {card.cardName} capture but current displayed target was {_currentTargetDisplayCard}");
            yield break;
        }
        
        yield return AnimateCardCaptured(
            _currentTargetDisplayCard.Transform,
            player.PlayerDisplayCard.transform
        );
        
        ClearCurrentTargetCard();
        UpdateRollButtonState();
    }

    private void ClearCurrentEventCard()
    {
        if (_currentEventDisplayCard == null) return;
        
        if (_currentEventDisplayCard && _currentEventDisplayCard.gameObject)
        {
            Destroy(_currentEventDisplayCard.gameObject);
            _currentEventDisplayCard = null;
        }
        
        SetEventButtonsInteractable(false);
    }
    
    private void ClearCurrentTargetCard()
    {
        if (_currentTargetDisplayCard.IsNull()) return;
        
        _currentTargetDisplayCard.Clear();
    }
    
    private void OnClickEventApply()
    {
        TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.ApplyEventCardRequest, _currentEventDisplayCard.GetCard()));
    }

    private IEnumerator HandleEventApplied()
    {
        yield return AnimateEventApplied(_currentEventDisplayCard.gameObject);
    }

    private IEnumerator HandleChangeToEventScreen()
    {
        ClearCurrentEventCard();
        UpdateRollButtonState();
        
        yield return AnimateFadeOutScreen();

        isScreenActive = false;
    }

    private IEnumerator HandleChangeFromEventScreen()
    {
        yield return eventUI.WaitUntilScreenState(false);

        isScreenActive = true;
        
        yield return AnimateFadeInScreen();
    }

    private void OnClickEventSave()
    {
        TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.SaveEventCardRequest , _currentEventDisplayCard.GetCard()));
    }

    private IEnumerator EventSaved(EventCard card)
    {
        if (_currentEventDisplayCard.GetCard() != card)
        {
            Debug.LogWarning($"Tried to animate {card.cardName} save but current displayed event was {_currentEventDisplayCard}");
            yield break;
        }
        
        yield return AnimateEventSaved(
            _currentEventDisplayCard.gameObject,
            _currentPlayerDisplayCard.transform
        );
        
        UpdateRollButtonState();
    }
    
#endregion Card Feedback

    #region Animations

    private IEnumerator AnimateFadeInScreen()
    {
        bool done = false;

        // Ensure starting state (invisible + shrunk)
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        gameObject.transform.localScale = Vector3.one * 0.9f;

        Sequence seq = DOTween.Sequence();
        seq.Append(canvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutCubic));
        seq.Join(gameObject.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));

        seq.OnComplete(() => done = true);

        while (!done)
            yield return null;

        // Ensure final state is perfect
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        gameObject.transform.localScale = Vector3.one;
    }
    private IEnumerator AnimateFadeOutScreen()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        bool done = false;

        Sequence seq = DOTween.Sequence();
        seq.Append(canvasGroup.DOFade(0f, 0.3f).SetEase(Ease.InCubic));
        seq.Join(gameObject.transform.DOScale(0.9f, 0.3f).SetEase(Ease.InBack));

        seq.OnComplete(() => done = true);

        while (!done)
            yield return null;

        canvasGroup.alpha = 0f;
        gameObject.transform.localScale = Vector3.one;
    }
    
    private IEnumerator AnimateCardSpawn(Transform card, float delay)
    {
        bool done = false;

        card.localScale = Vector3.zero;

        card.DOScale(1f, cardSpawnDuration)
            .SetEase(Ease.OutBack)
            .SetDelay(delay)
            .OnComplete(() => done = true);

        while (!done)
            yield return null;
    }
    
    private IEnumerator AnimateEventApplied(GameObject eventGO)
    {
        bool done = false;

        var t = eventGO.transform;

        var seq = DOTween.Sequence();
        seq.Append(t.DOShakePosition(0.3f, 15f, 10, 90, false, true));
        seq.Join(t.DOScale(1.2f, 0.15f).SetLoops(2, LoopType.Yoyo));
        seq.Append(t.DOScale(0f, 0.25f).SetEase(Ease.InBack));
        seq.OnComplete(() => done = true);

        while (!done)
            yield return null;
    }

    private IEnumerator AnimateCardCaptured(Transform card, Transform target)
    {
        bool done = false;

        var seq = DOTween.Sequence();
        seq.Append(card.DOMove(target.position, 0.5f).SetEase(Ease.InBack));
        seq.Join(card.DOScale(1.3f, 0.25f).SetLoops(2, LoopType.Yoyo));
        seq.OnComplete(() => done = true);

        while (!done)
            yield return null;
    }

    private IEnumerator AnimateEventSaved(GameObject eventGO, Transform target)
    {
        bool done = false;

        var t = eventGO.transform;

        var seq = DOTween.Sequence();
        seq.Append(t.DOMove(target.position, 0.5f).SetEase(Ease.InBack));
        seq.Join(t.DOScale(1.3f, 0.25f).SetLoops(2, LoopType.Yoyo));
        seq.OnComplete(() =>
            {
                done = true;
                ClearCurrentEventCard();
            });

        while (!done)
            yield return null;
    }
    
    #endregion

    #region Testing Helper

    private void InitializePlayersForTesting()
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

    #region Buttons
    
    private void UpdateRollButtonState()
    {
        // 🔒 Disable by default
        bool enable = false;

        // Conditions:
        // - Not AI
        // - Not while an event is active
        // - Player has either played or saved their event
        if (!isAIPlayer)
        {
            var eventManager = _eventManager ?? _mainPhase.EventManager;

            bool eventInactive = !eventManager.IsEventScreen;
            bool playerHasResolvedEvent = _currentEventDisplayCard == null;
            bool targetCardDrawn = !_currentTargetDisplayCard.IsNull();
            
            bool canRoll = currentPlayer.CanRoll();

            enable = eventInactive && playerHasResolvedEvent && canRoll && targetCardDrawn;
        }
        
        EnableDiceButton(enable);
    }

    #endregion
   
}
