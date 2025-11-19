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

    private bool _playerResolvedEvent = false;
    private bool _noMoreEventCards = false;

    private GM_MainPhase _mainPhase;
    private EventManager _eventManager;

    #region Phase Generics
    protected override void OnPhaseEnabled()
    {
        _mainPhase = game.mainPhase;
        
        ClearCurrentEventCard();
        ClearCurrentTargetCard();
        
        if (_mainPhase == null)
        {
            Debug.LogError("MainPhaseGameManager not found. Ensure it's initialized before UI.");
            return;
        }
        
        _eventManager = _mainPhase.EventManager;
        
        eventUI.Initialize();
        
        EnqueueUI(PhaseEnabledRoutine());
        
        //TODO: dont forget to remove
        InitializePlayersForTesting();
        
        EnableDrawButtons(false);
        SetEventButtonsInteractable(false);
        
        base.OnPhaseEnabled();
    }

    private IEnumerator PhaseEnabledRoutine()
    {
        if (gameUI.previouslyActiveUI)
            yield return gameUI.previouslyActiveUI.WaitUntilScreenState(false);
        
        //TODO: dont forget to uncomment
        // RelocatePlayerCards(playerUIParent);
    }
    
    protected override void SubscribeToPhaseEvents()
    {
        base.SubscribeToPhaseEvents();
        
        TurnFlowBus.Instance.OnOneTimeEvent += HandleRaisedOnce;
        
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

    #endregion
    
    #region BusEvents

    private void HandleRaisedOnce(IGameEvent e)
    {
        if (e is not MainStageEvent m) return;
        if (m.stage != MainStage.NoMoreEventCards) return;
        
        drawEventButton.interactable = false;
        drawEventButton.onClick.RemoveAllListeners();
        
        _noMoreEventCards = true;
        
        TurnFlowBus.Instance.OnOneTimeEvent -= HandleRaisedOnce;
    }

    protected override void HandleTurnEvent(IGameEvent e)
    {
        base.HandleTurnEvent(e);

        if (!isCurrent) return;
        
        switch (e)
        {
            case MainStageEvent m:
                HandleMainStageEvent(m);
                break;
            
            case EventCardEvent c:
                HandleEventCardEvent(c);
                break;
        }
    }

    private void HandleEventCardEvent(EventCardEvent c)
    {
        switch (c.stage)
        {
            case EventStage.EventApplied:
                HandleEventApplied();
                break;
                
            case EventStage.ChangeToEventScreen:
                HandleChangeToEventScreenRoutine();
                break;
                    
            case EventStage.DuelCompleted:
                EnqueueUI(HandleChangeFromEventScreen());
                break;
        }
    }

    private void HandleMainStageEvent(MainStageEvent m)
    {
        switch (m.stage)
        {
            case MainStage.EventCardDrawn:
                SpawnEventCardRoutine((EventCard)m.payload);
                break;
                
            case MainStage.TargetCardDrawn:
                SpawnTargetCardRoutine((Card)m.payload);
                break;
                
            case MainStage.EventCardSaved:
                EventSavedRoutine((EventCard)m.payload);
                break;
                
            case MainStage.CardCaptured:
                var captured = (CardCapturedData)m.payload;
                CardCapturedRoutine(captured.player, captured.card);
                break;
                
            case MainStage.StateDiscarded:
                var discarded = (StateCard)m.payload;
                EnqueueUI(StateDiscardedRoutine(discarded));
                break;
        }
    }

    #endregion

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
    
    private void TogglePlayerPanel()
    {
        ownedCardsPanel.TogglePanel();
    }

    #endregion Player Management

    #region Turn Flow

    protected override void OnPlayerTurnStarted(Player player)
    {
        _playerResolvedEvent = _noMoreEventCards;

        base.OnPlayerTurnStarted(player);
        
        EnqueueUI(OnPlayerTurnStartedRoutine(player));
    }

    private IEnumerator OnPlayerTurnStartedRoutine(Player player)
    {
        _currentPlayerDisplayCard = player.PlayerDisplayCard;
        
        if (!_currentTargetDisplayCard.IsNull() && _noMoreEventCards)
        {
            yield return EnableDiceButtonRoutine(true);
        }
        
        EnableDrawButtons(true);
    }

    protected override void OnPlayerTurnEnded(Player player)
    {
       base.OnPlayerTurnEnded(player);

       EnqueueUI(OnPlayerTurnEndedRoutine(player));
    }

    private IEnumerator OnPlayerTurnEndedRoutine(Player player)
    { 
        _currentPlayerDisplayCard = null;
        player.PlayerDisplayCard.ShowDice(false);
        yield break;
    }
    
    protected override void OnPlayerRolledDice(Player player, int roll)
    {
        if (!player.CanRoll())
            EnableDiceButton(false);
        
        base.OnPlayerRolledDice(player, roll);
    }

    #endregion Turn Flow
    
    #region Card Spawning
    
    private void OnSpawnTargetClicked()
    {
        TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.DrawTargetCardRequest));
    }

    private void SpawnTargetCardRoutine(Card card)
    {
        if (card == null) return;
        drawTargetButton.interactable = false;
        
        EnqueueUI(SpawnTargetCard(card));
    }
    private IEnumerator SpawnTargetCard(Card card)
    {
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

        if (_noMoreEventCards)
            yield return EnableDiceButtonRoutine(true);
    }
    
    private void OnSpawnEventClicked()
    {
        TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.DrawEventCardRequest));
    }

    private void SpawnEventCardRoutine(EventCard card)
    {
        drawEventButton.interactable = false;
        EnqueueUI(SpawnEventCard(card));
    }
    private IEnumerator SpawnEventCard(EventCard card)
    {
        var eventDisplay = Instantiate(eventCardPrefab, eventArea);
        _currentEventDisplayCard = eventDisplay.GetComponent<EventDisplayCard>();
        _currentEventDisplayCard?.SetCard(card);
        
        yield return AnimateCardSpawn(eventDisplay.transform, 0.1f);
    }

    #endregion Card Spawning
    
    #region Card Feedback

    private void CardCapturedRoutine(Player player, Card card)
    {
        if (!_currentTargetDisplayCard.IsTarget(card))
        {
            Debug.LogWarning($"Tried to capture {card.cardName} but current displayed target was {_currentTargetDisplayCard}");
            return;
        }
        EnableDiceButton(false);
        
        EnqueueUI(CardCaptured(player, card));
        
    }
    private IEnumerator CardCaptured(Player player, Card card)
    {
        yield return AnimateCardCaptured(
            _currentTargetDisplayCard.Transform,
            player.PlayerDisplayCard.transform
        );
        
        ClearCurrentTargetCard();
    }
    
    private void ClearCurrentTargetCard()
    {
        if (_currentTargetDisplayCard.IsNull()) return;
        
        _currentTargetDisplayCard.Clear();
    }
    
    private void OnClickEventSave()
    {
        TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.SaveEventCardRequest , _currentEventDisplayCard.GetCard()));
    }

    private void EventSavedRoutine(EventCard card)
    {
        if (_currentEventDisplayCard.GetCard() != card)
        {
            Debug.LogWarning($"Tried to save {card.cardName} but current displayed event was {_currentEventDisplayCard}");
            return;
        }
        _playerResolvedEvent = true;
        EnqueueUI(EventSaved(card));
    }
    private IEnumerator EventSaved(EventCard card)
    {
       yield return AnimateEventSaved(
            _currentEventDisplayCard.gameObject,
            _currentPlayerDisplayCard.transform
        );

        EnableDiceButton(true);
    }
    
    private void OnClickEventApply()
    {
        TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.ApplyEventCardRequest, _currentEventDisplayCard.GetCard()));
    }
    
    private void HandleEventApplied()
    {
        _playerResolvedEvent = true;
        EnqueueUI(AnimateEventApplied(_currentEventDisplayCard.gameObject));
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

    private void HandleChangeToEventScreenRoutine()
    {
        EnableDiceButton(false);
        EnqueueUI(HandleChangeToEventScreen());
    }
    private IEnumerator HandleChangeToEventScreen()
    {
        ClearCurrentEventCard();
        
        yield return AnimateFadeOutScreen();
    }

    private IEnumerator HandleChangeFromEventScreen()
    {
        yield return eventUI.WaitUntilScreenState(false);
        
        yield return AnimateFadeInScreen();
    }
    
    private IEnumerator StateDiscardedRoutine(StateCard discarded)
    {
        if (!_currentTargetDisplayCard.IsTarget(discarded)) yield break;

        yield return CurrentTargetDiscardedAnimation();
    }
    
#endregion Card Feedback

    #region Animations

    private IEnumerator AnimateFadeInScreen()
    {
        isScreenActive = true;
        
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
        
        isScreenActive = false;
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
        
        SetEventButtonsInteractable(true);
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
        
        ClearCurrentEventCard();

        //TODO: consider this more
        if (!_eventManager.IsEventScreen)
        {
            yield return EnableDiceButtonRoutine(true);
        }
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
    
    private IEnumerator CurrentTargetDiscardedAnimation()
    {
        if (_currentTargetDisplayCard.IsNull()) yield break;

        var t = _currentTargetDisplayCard.Transform;
        t.DOKill();

        bool done = false;

        Sequence s = DOTween.Sequence();

        // 1. Quick shrink shock
        s.Append(t.DOScale(0.9f, 0.15f).SetEase(Ease.OutBack));

        // 2. Spin + shrink
        s.Append(t.DORotate(new Vector3(0, 0, 180f), 0.35f, RotateMode.FastBeyond360)
            .SetEase(Ease.InCubic));
        s.Join(t.DOScale(0f, 0.35f).SetEase(Ease.InBack));

        // 3. Cleanup
        s.OnComplete(() =>
        {
            ClearCurrentTargetCard();
            done = true;
        });

        // 🚀 Now Unity waits until animation really ends
        while (!done)
            yield return null;
    }

    
    #endregion
    
    #region Button Helpers
    
    private void EnableDrawButtons(bool enable)
    {
        if (_currentTargetDisplayCard.IsNull())
            drawTargetButton.interactable = enable && !isAIPlayer;
        
        drawEventButton.interactable = enable && !isAIPlayer && !_noMoreEventCards;
    }

    private IEnumerator UpdateRollButtonState()
    {
        Debug.Log("rolled");
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
            bool playerHasResolvedEvent = _playerResolvedEvent;
            bool targetCardDrawn = !_currentTargetDisplayCard.IsNull();
            
            bool canRoll = currentPlayer.CanRoll();
            
            enable = eventInactive && playerHasResolvedEvent && canRoll && targetCardDrawn;
        }
        
        EnableDiceButton(enable);
        yield break;
    }
    
    private void SetEventButtonsInteractable(bool interactable)
    {
        bool canSave = false;

        if (_noMoreEventCards)
            interactable = false;
        
        if (isAIPlayer)
            interactable = false;
        
        if (_currentTargetDisplayCard.IsNull()) 
            interactable = false;
        
        if (_playerResolvedEvent) 
            interactable = false;
        
        else if (interactable)
            canSave = _currentEventDisplayCard.GetCard().canSave;
        

        saveEventButton.interactable = canSave;
        
        playEventButton.interactable = interactable;
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
   
}
