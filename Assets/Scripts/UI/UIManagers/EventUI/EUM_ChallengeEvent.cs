
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
    private GameObject _targetDisplay;
    
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

    private void Awake()
    {
        eventScreen.SetActive(false);
        statesScreen.SetActive(false);
        duelScreen.SetActive(false);
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
            yield return _mainUI.WaitUntilUIQueueFree();
        
        eventScreen.SetActive(true);
        
        switch (e.stage)
        {
            case EventStage.ChallengeStatesDetermined:
            {
                var payload = (ChallengeStatesData)e.payload;
                // EnqueueUI(AnimateFadeInEventScreen());
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

            case EventStage.PlayerRolled:
            {
                var p = (PlayerRolledData)e.payload;
                OnPlayerRolled(p.Player, p.Roll);
                break;
            }
            
            case EventStage.CardOwnerChanged:
                var data = (CardOwnerChangedData)e.payload;
                EnqueueUI(CardOwnerChangedRoutine(data.owner,data.newOwner,data.card));
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
        EnqueueUI(ShowAltStatesRoutine(player, card1, card2));
    }

    private IEnumerator ShowAltStatesRoutine(Player player, StateCard card1, StateCard card2)
    {
        duelScreen.SetActive(true);

        CreateCardInTransform<PlayerDisplayCard>(player.PlayerDisplayCard.gameObject, midCardUI, player.assignedActor);
        CreateCardInTransform<StateDisplayCard>(_mainUI.stateCardPrefab, leftCardUI, card1);
        CreateCardInTransform<StateDisplayCard>(_mainUI.stateCardPrefab, rightCardUI, card2);

        yield return AnimateEventUIRoutine();
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
        
        statesScreen.SetActive(true);

        SelectableCardBus.Instance.OnEvent += HandleCardInputEvent;

        CreateChallengeStatesUI(statesToDisplay, spacingBetweenStateCards);

        yield return AnimateEventUIRoutine();

        rollDiceButton.interactable = !AIManager.Instance.IsAIPlayer(attacker);
        
        yield return new WaitForSeconds(2.5f);
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
        if (_challengeStates)
        {
            SelectableCardBus.Instance.OnEvent -= HandleCardInputEvent;
            
            EnqueueUI(AnimateFadeOutEventScreen());
            statesScreen.SetActive(false);
            
            foreach (Transform child in stateCardsUIParent)
                Destroy(child.gameObject);

            _challengeStates = false;
        }
        
        // attacker card
        CreateCardInTransform<PlayerDisplayCard>(attacker.PlayerDisplayCard.gameObject, leftCardUI, attacker.assignedActor);

        // defender card
        if (defender)
            CreateCardInTransform<PlayerDisplayCard>(defender.PlayerDisplayCard.gameObject, rightCardUI, defender.assignedActor);

        ClearCurrentTargetDisplay();
        
        // mid card
        switch (chosenCard)
        {
            case StateCard s:
                _targetDisplay = CreateCardInTransform<StateDisplayCard>(_mainUI.stateCardPrefab, midCardUI, s);
                break;
            case InstitutionCard inst:
                _targetDisplay = CreateCardInTransform<InstitutionDisplayCard>(_mainUI.institutionCardPrefab, midCardUI, inst);
                break;
        }

        rollDiceButton.interactable = !AIManager.Instance.IsAIPlayer(attacker);
        
        rollDiceButton.onClick.RemoveAllListeners();
        rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        
        yield return AnimateEventUIRoutine();
        duelScreen.SetActive(true);
    }

    private void ClearCurrentTargetDisplay()
    {
        if (_targetDisplay)
        {
            Destroy(_targetDisplay);
            _targetDisplay = null;
        }
    }
    
    private IEnumerator CardOwnerChangedRoutine(Player owner,Player newOwner, Card card)
    {
        yield return AnimateCardCaptured(
            _targetDisplay.transform,
            newOwner.PlayerDisplayCard.transform
        );
        
        ClearCurrentTargetDisplay();
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

        Canvas.ForceUpdateCanvases();
    }
    #endregion
    
    private GameObject CreateCardInTransform<T>(GameObject prefab, Transform uiParent, Card cardToSet)
        where T : MonoBehaviour, IDisplayCard
    {
        foreach (Transform child in uiParent)
            Destroy(child.gameObject);

        if (cardToSet == null) return null;
        
        var go = Instantiate(prefab, uiParent);
        go.SetActive(true);

        if (go.TryGetComponent(out T displayCard))
        {
            displayCard.SetCard(cardToSet);
        }
        else
        {
            Debug.LogError($"{prefab.name} is missing {typeof(T).Name} component.");
        }

        if (go.TryGetComponent(out RectTransform rt))
            rt.anchoredPosition = Vector2.zero;

        return go;
    }
    
    private void ReturnToMainPhaseUI()
    {
        rollDiceButton.onClick.RemoveAllListeners();
        
        EnqueueUI(ReturnToMainPhaseUIRoutine());
    }

    private IEnumerator ReturnToMainPhaseUIRoutine()
    {
        yield return new WaitForSeconds(2.5f);
        
        // Fade out event screen if needed
        yield return AnimateFadeOutEventScreen();
        
        ClearCurrentTargetDisplay();
        
        _highlightedCard = null;

        eventScreen.SetActive(false);
        duelScreen.SetActive(false);
        statesScreen.SetActive(false);
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
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
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
        
        yield return new WaitForSeconds(2.5f);
    }
}
