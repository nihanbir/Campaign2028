
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI for Challenge/AltStates/Duel. Now reacts primarily to GameEventBus.
/// Keeps existing animations and visuals intact.
/// </summary>
public class EUM_ChallengeEvent : MonoBehaviour
{
    [Header("Challenge Any State")]
    [SerializeField] private GameObject statesScreen;
    [SerializeField] private Transform stateCardsUIParent;
    [SerializeField] private float spacingBetweenStateCards = 150f;
    
    [Header("Duel")]
    [SerializeField] private GameObject duelScreen;
    [SerializeField] private Transform rightCardUI;
    [SerializeField] private Transform leftCardUI;
    [SerializeField] private Transform midCardUI;
    
    [Header("Event Screen")]
    [SerializeField] protected GameObject eventScreen;
    [SerializeField] protected Button rollDiceButton;
    [SerializeField] protected Image diceImage;
    
    [Header("CanvasGroup")] 
    [SerializeField] private CanvasGroup canvasGroup;

    private GM_MainPhase _mainPhase;
    private UM_MainPhase _mainUI;
    
    private StateDisplayCard _highlightedCard;

    private bool _challengeStates = false;
    private bool _altStates = false;
    private bool _isActive = false;
    private bool _cardCaptured = false;
    
    private readonly Queue<IEnumerator> _queue = new();
    private bool _queueRunning;

    private void EnqueueUI(IEnumerator routine)
    {
        _queue.Enqueue(routine);
        if (!_queueRunning)
            StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        _queueRunning = true;

        while (_queue.Count > 0)
            yield return StartCoroutine(_queue.Dequeue());
    
        _queueRunning = false;
    }

    public IEnumerator WaitUntilQueueFree()
    {
        while (_queueRunning)
            yield return null;
    }

    public IEnumerator WaitUntilScreenState(bool active)
    {
        if (active)
        {
            while (!_isActive)
                yield return null;
        }
        else
        {
            while (_isActive)
                yield return null;
        }
    }

    private void Awake()
    {
        eventScreen.SetActive(false);
        statesScreen.SetActive(false);
        duelScreen.SetActive(false);
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void Initialize()
    {
        _mainPhase = GameManager.Instance?.mainPhase;
        if (_mainPhase == null)
        {
            Debug.LogError("MainPhaseGameManager not found. Ensure it's initialized before UI.");
            return;
        }

        _mainUI = GameUIManager.Instance.mainUI;
        
        EventCardBus.Instance.OnEvent += HandleGameEvent;
        
        if (rollDiceButton)
        {
            rollDiceButton.onClick.RemoveAllListeners();
            rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        }
    }

    private void HandleGameEvent(EventCardEvent e)
    {
        StartCoroutine(HandleGameEventRoutine(e));
    }
    
    private IEnumerator HandleGameEventRoutine(EventCardEvent e)
    {
        // 🔥 WAIT FOR MAIN PHASE UI ANIMATIONS to finish
        if (_mainUI != null)
            yield return _mainUI.WaitUntilScreenState(false);
        
        eventScreen.SetActive(true);
        _isActive = true;
        
        switch (e.stage)
        {
            case EventStage.ChallengeStatesDetermined:
            {
                var payload = (ChallengeStatesData)e.payload;
                ShowStateCards(payload.Player, payload.States);
                break;
            }

            case EventStage.DuelStarted:
            {
                var duel = (DuelData)e.payload;
                ShowDuel(duel.Attacker, duel.Defender, duel.ChosenCard);
                break;
            }

            case EventStage.AltStatesShown:
            {
                var p = (AltStatesData)e.payload;
                ShowAltStates(p.Player, p.State1, p.State2);
                break;
            }
            
            case EventStage.StateDiscarded:
                var d = (StateCard)e.payload;
                EnqueueUI(StateDiscardedRoutine(d));
                break;

            case EventStage.PlayerRolled:
            {
                var p = (PlayerRolledData)e.payload;
                OnPlayerRolled(p.Player, p.Roll);
                break;
            }
            
            case EventStage.CardOwnerChanged:
                _cardCaptured = true;
                var data = (CardOwnerChangedData)e.payload;
                var newOwnerTransform = data.newOwner.PlayerDisplayCard.transform;
                EnqueueUI(MoveCardToOwnerRoutine(newOwnerTransform));
                break;
            
            case EventStage.DuelCompleted:
                ReturnToMainPhaseUI();
                break;
        }
    }
    
    private void HandleCardInputEvent(CardInputEvent e)
    {
        if (!eventScreen.activeSelf) return;
        
        switch (e.stage)
        {
            case CardInputStage.Clicked:
                ToggleHighlightedCard((StateDisplayCard)e.payload);
                break;
        }
    }

    private IEnumerator AnimateEventUIRoutine()
    {
        canvasGroup.alpha = 0f;
        eventScreen.transform.localScale = Vector3.one * 0.85f;

        bool done = false;

        Sequence seq = DOTween.Sequence();
        seq.Append(canvasGroup.DOFade(1f, 0.4f).SetEase(Ease.OutCubic));
        seq.Join(eventScreen.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
        seq.OnComplete(() => done = true);

        while (!done)
            yield return null;
    }

    
    private void OnRollDiceClicked()
    {
        EventCardBus.Instance.Raise(
            new EventCardEvent(EventStage.RollDiceRequest)
        );
    }
    
    private void OnPlayerRolled(Player player, int roll)
    {
        diceImage.sprite = GameUIManager.Instance.diceFaces[roll - 1];
        EnqueueUI(DicePopAnimation(diceImage));
    }
    

    #region AltStates
    private void ShowAltStates(Player player, StateCard card1, StateCard card2)
    {
        _altStates = true;
        EnqueueUI(ShowAltStatesRoutine(player, card1, card2));
    }

    private IEnumerator ShowAltStatesRoutine(Player player, StateCard card1, StateCard card2)
    {
        duelScreen.SetActive(true);

        CreateCardInTransform<PlayerDisplayCard>(player.PlayerDisplayCard.gameObject, midCardUI, player.assignedActor);
        CreateCardInTransform<StateDisplayCard>(_mainUI.stateCardPrefab, leftCardUI, card1);
        CreateCardInTransform<StateDisplayCard>(_mainUI.stateCardPrefab, rightCardUI, card2);

        yield return AnimateEventUIRoutine();
        
        var isAIPlayer = AIManager.Instance.IsAIPlayer(player);
        
        rollDiceButton.onClick.RemoveAllListeners();
        rollDiceButton.onClick.AddListener(OnRollDiceClicked);

        rollDiceButton.interactable = !isAIPlayer;
        
        canvasGroup.interactable = !isAIPlayer;
        canvasGroup.blocksRaycasts = !isAIPlayer;
    }

    #endregion
    
    #region Challenge State

    private void ShowStateCards(Player attacker, List<StateCard> statesToDisplay)
    {
        EnqueueUI(ShowStateCardsRoutine(attacker, statesToDisplay));
    }

    private IEnumerator ShowStateCardsRoutine(Player attacker, List<StateCard> statesToDisplay)
    {
        _challengeStates = true;
        
        SelectableCardBus.Instance.OnEvent += HandleCardInputEvent;

        CreateChallengeStatesUI(statesToDisplay, spacingBetweenStateCards);
        
        statesScreen.SetActive(true);
        yield return AnimateFadeInEventScreen();

        var isAIPlayer = AIManager.Instance.IsAIPlayer(attacker);

        rollDiceButton.interactable = !isAIPlayer;
        
        canvasGroup.interactable = !isAIPlayer;
        canvasGroup.blocksRaycasts = !isAIPlayer;
    }

    private StateDisplayCard FindSelectedState(Card card)
    {
        if (card == null || stateCardsUIParent.childCount == 0)
            return null;

        if (card is not StateCard stateCard) 
            return null;
        
        for (int i = 0; i < stateCardsUIParent.childCount; i++)
        {
            var child = stateCardsUIParent.GetChild(i);
            
            if (!child.TryGetComponent(out StateDisplayCard display)) 
                continue;
            
            // Match the underlying card
            if (display.GetCard() == stateCard)
                return display;
        }
        
        return null;
    }
    
    private GameObject FindDiscardedStateCard(StateCard discarded)
    {
        // Right side
        if (rightCardUI.childCount > 0)
        {
            var child = rightCardUI.GetChild(0);
            if (child.TryGetComponent(out StateDisplayCard stateDisplay))
            {
                if (stateDisplay.GetCard() == discarded)
                    return child.gameObject;
            }
        }

        // Left side
        if (leftCardUI.childCount > 0)
        {
            var child = leftCardUI.GetChild(0);
            if (child.TryGetComponent(out StateDisplayCard stateDisplay))
            {
                if (stateDisplay.GetCard() == discarded)
                    return child.gameObject;
            }
        }

        return null;
    }
    
    private void ToggleHighlightedCard(StateDisplayCard card)
    {
        if (_highlightedCard)
            _highlightedCard.SetIsSelected(false);
        
        _highlightedCard = card;
        _highlightedCard?.SetIsSelected(true);
    }

    private void ShowDuel(Player attacker, Player defender, Card chosenCard)
    {
        EnqueueUI(ShowDuelRoutine(attacker, defender, chosenCard));
    }

    private IEnumerator ShowDuelRoutine(Player attacker, Player defender, Card chosenCard)
    {
        var isAIPlayer = AIManager.Instance.IsAIPlayer(attacker);
        
        if (_challengeStates)
        {
            SelectableCardBus.Instance.OnEvent -= HandleCardInputEvent;

            if (isAIPlayer) 
                _highlightedCard = FindSelectedState(chosenCard);
            
            yield return PlayZoomPulse(_highlightedCard.transform);
            
            yield return AnimateFadeOutEventScreen();
            statesScreen.SetActive(false);
            
            foreach (Transform child in stateCardsUIParent)
                Destroy(child.gameObject);

            _challengeStates = false;
        }
        
        // attacker card
        CreateCardInTransform<PlayerDisplayCard>(attacker.PlayerDisplayCard.gameObject, leftCardUI, attacker.assignedActor);

        // defender card
        if (defender != null)
            CreateCardInTransform<PlayerDisplayCard>(defender.PlayerDisplayCard.gameObject, rightCardUI, defender.assignedActor);
        
        duelScreen.SetActive(true);
        yield return AnimateFadeInEventScreen();

        GameObject targetDisplay = null;
        
        // mid card
        switch (chosenCard)
        {
            case StateCard s:
                targetDisplay = CreateCardInTransform<StateDisplayCard>(_mainUI.stateCardPrefab, midCardUI, s);
                break;
            case InstitutionCard inst:
                targetDisplay = CreateCardInTransform<InstitutionDisplayCard>(_mainUI.institutionCardPrefab, midCardUI, inst);
                break;
        }

        yield return AnimateCardMoveToMid(
            targetDisplay,
            rightCardUI as RectTransform, 
            midCardUI as RectTransform
        );
        
        rollDiceButton.onClick.RemoveAllListeners();
        rollDiceButton.onClick.AddListener(OnRollDiceClicked);

        rollDiceButton.interactable = !isAIPlayer;
        
        canvasGroup.interactable = !isAIPlayer;
        canvasGroup.blocksRaycasts = !isAIPlayer;
    }
    
    private IEnumerator PlayZoomPulse(Transform target)
    {
        Vector3 original = target.localScale;
        
        bool done = false;
        
        Sequence seq = DOTween.Sequence();
        seq.Append(target.DOScale(original * 1.1f, 0.15f).SetEase(Ease.OutQuad));
        seq.Append(target.DOScale(original, 0.15f).SetEase(Ease.InQuad));
        seq.OnComplete(() => done = true);

        while (!done)
            yield return null;
    }
    
    private IEnumerator MoveCardToOwnerRoutine(Transform owner)
    {
        GameObject targetDisplay = null;

        if (midCardUI.childCount > 0)
            targetDisplay = midCardUI.GetChild(0).gameObject;

        if (!targetDisplay) 
            yield break;
        
        yield return AnimateCardCaptured(
            targetDisplay.transform,
            owner
        );
        
        Destroy(targetDisplay);
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
    
    private IEnumerator StateDiscardedRoutine(StateCard discarded)
    {
        GameObject targetDisplay = FindDiscardedStateCard(discarded);

        if (!targetDisplay)
            yield break;

        yield return CurrentTargetDiscardedAnimation(targetDisplay);
    }
    
    private IEnumerator CurrentTargetDiscardedAnimation(GameObject card)
    {
        if (!card) yield break;

        var t = card.transform;
        t.DOKill();

        bool done = false;

        Sequence s = DOTween.Sequence();

        // 1. Quick shrink shock
        s.Append(t.DOScale(0.9f, 0.15f).SetEase(Ease.OutBack));

        // 2. Spin + shrink
        s.Append(t.DORotate(new Vector3(0, 0, 180f), 0.35f, RotateMode.FastBeyond360)
            .SetEase(Ease.InCubic));
        s.Join(t.DOScale(0f, 0.35f).SetEase(Ease.InBack));

        s.OnComplete(() =>
        {
            done = true;
            Destroy(card);
        });

        // 🚀 Now Unity waits until animation really ends
        while (!done)
            yield return null;
    }
    
    private IEnumerator AnimateCardMoveToMid(GameObject cardGO, RectTransform fromUI, RectTransform toUI)
    {
        RectTransform cardRT = cardGO.transform as RectTransform;

        // 1. Cache final mid-world position BEFORE reparenting
        Vector3 midWorldPos = toUI.position;

        // 2. Reparent card to eventScreen (top-level), but do NOT preserve world position
        //    This prevents layout snapping back to (0,0)
        cardRT.SetParent(eventScreen.transform, worldPositionStays: false);

        // 3. Move card to attacker/defender world position
        cardRT.position = fromUI.position;

        // 4. Animate card from defender → mid (using world-space)
        bool done = false;
        Sequence seq = DOTween.Sequence();

        seq.Append(cardRT.DOMove(midWorldPos, 0.4f).SetEase(Ease.OutCubic));
        seq.Join(cardRT.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack));

        seq.OnComplete(() => done = true);

        while (!done)
            yield return null;

        // 5. Snap card under midCardUI correctly after animation
        cardRT.SetParent(toUI, worldPositionStays: false);
        cardRT.anchoredPosition = Vector2.zero;
        cardRT.localScale = Vector3.one;
    }
    
    private void CreateChallengeStatesUI(List<StateCard> statesToDisplay, float spacing, float verticalSpacing = 40f)
    {
        int count = statesToDisplay.Count;
        if (count == 0) return;
        
        for (int i = 0; i < count; i++)
        {
            GameObject uiInstance = Instantiate(_mainUI.stateCardPrefab, stateCardsUIParent);
            if (!uiInstance.TryGetComponent(out StateDisplayCard displayCard))
            {
                Debug.LogError("CardDisplayPrefab missing StateDisplayCard component.");
                continue;
            }

            displayCard.SetCard(statesToDisplay[i]);
            
            displayCard.SetClickable(true);
            displayCard.SetHoldable(true);
            
        }

        // Canvas.ForceUpdateCanvases();
    }
    #endregion
    
    private GameObject CreateCardInTransform<T>(GameObject prefab, Transform uiParent, Card cardToSet)
        where T : MonoBehaviour, IDisplayCard
    {
        foreach (Transform child in uiParent)
            Destroy(child.gameObject);

        if (cardToSet == null) return null;
        
        var go = Instantiate(prefab, uiParent, false);

        if (go.TryGetComponent(out T displayCard))
        {
            displayCard.SetCard(cardToSet);
        }
        else
        {
            Debug.LogError($"{prefab.name} is missing {typeof(T).Name} component.");
        }
        
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        return go;
    }
    
    private void ReturnToMainPhaseUI()
    {
        rollDiceButton.onClick.RemoveAllListeners();

        EnqueueUI(ReturnToMainPhaseUIRoutine());
    }

    private IEnumerator ReturnToMainPhaseUIRoutine()
    {
        if (!_cardCaptured && !_altStates)
            yield return MoveCardToOwnerRoutine(rightCardUI);
            
        // Fade out event screen if needed
        yield return AnimateFadeOutEventScreen();
        
        _highlightedCard = null;
        _cardCaptured = false;
        _altStates = false;
        
        eventScreen.SetActive(false);
        duelScreen.SetActive(false);
        statesScreen.SetActive(false);
        
        _isActive = false;
    }
    
    private IEnumerator AnimateFadeOutEventScreen()
    {
        bool done = false;

        Sequence seq = DOTween.Sequence();
        seq.Append(canvasGroup.DOFade(0f, 0.3f).SetEase(Ease.InCubic));
        seq.Join(eventScreen.transform.DOScale(0.9f, 0.3f).SetEase(Ease.InBack));

        seq.OnComplete(() => done = true);

        while (!done)
            yield return null;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        eventScreen.transform.localScale = Vector3.one;
    }
    
    private IEnumerator AnimateFadeInEventScreen()
    {
        bool done = false;

        // Ensure starting state (invisible + shrunk)
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        eventScreen.transform.localScale = Vector3.one * 0.9f;

        Sequence seq = DOTween.Sequence();
        seq.Append(canvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutCubic));
        seq.Join(eventScreen.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));

        seq.OnComplete(() => done = true);

        while (!done)
            yield return null;

        // Ensure final state is perfect
        canvasGroup.alpha = 1f;
        eventScreen.transform.localScale = Vector3.one;
    }
    
    private IEnumerator DicePopAnimation(Image diceImg)
    {
        if (!diceImg) yield break;
        
        diceImg.gameObject.SetActive(true);

        diceImg.transform.DOKill();
        diceImg.transform.localScale = Vector3.zero;

        bool done = false;

        diceImg.transform
            .DOScale(1f, 0.35f)
            .SetEase(Ease.OutBack)
            .OnComplete(() => done = true);
        
        while (!done)
            yield return null;
        
    }
}
