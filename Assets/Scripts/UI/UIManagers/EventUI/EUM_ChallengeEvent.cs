
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
    
    private Player _currentPlayer;
    private StateDisplayCard _highlightedCard;
    
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
        switch (e.stage)
        {
            case EventStage.DuelCompleted:
                ReturnToMainPhaseUI();
                break;
            
            default:
                StartCoroutine(HandleGameEventRoutine(e));
                break;
        }
    }
    
    private IEnumerator HandleGameEventRoutine(EventCardEvent e)
    {
        // 🔥 WAIT FOR MAIN PHASE UI ANIMATIONS to finish
        if (_mainUI != null)
            yield return _mainUI.WaitUntilUIQueueFree();
        
        eventScreen.SetActive(true);
        
        switch (e.stage)
        {
            case EventStage.DuelCompleted:
            {
                ReturnToMainPhaseUI();
                break;
            }
            
            case EventStage.ChallengeStatesDetermined:
            {
                var payload = (ChallengeStatesData)e.payload;
                EnqueueUI(AnimateFadeInEventScreen());
                ShowStateCards(payload.Player, payload.States);
                break;
            }

            case EventStage.DuelStarted:
            {
                var duel = (DuelData)e.payload;
                EnqueueUI(AnimateFadeInEventScreen());
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
        }
    }

    private IEnumerator AnimateEventUIRoutine()
    {
        CanvasGroup cg = eventScreen.GetComponent<CanvasGroup>();
        if (!cg) cg = eventScreen.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        eventScreen.transform.localScale = Vector3.one * 0.85f;

        bool done = false;

        Sequence seq = DOTween.Sequence();
        seq.Append(cg.DOFade(1f, 0.4f).SetEase(Ease.OutCubic));
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
        _currentPlayer = player;
        
        duelScreen.SetActive(true);

        CreateCardInTransform<PlayerDisplayCard>(player.PlayerDisplayCard.gameObject, midCardUI, player.assignedActor);
        CreateCardInTransform<StateDisplayCard>(_mainUI.stateCardPrefab, leftCardUI, card1);
        CreateCardInTransform<StateDisplayCard>(_mainUI.stateCardPrefab, rightCardUI, card2);

        yield return AnimateEventUIRoutine();
    }

    #endregion
    
    #region Challenge State
    private void HandleCardHeld()
    {
        foreach (Transform child in stateCardsUIParent)
            if (child.TryGetComponent(out StateDisplayCard display))
                display.SetClickable(false);
        
        StateDisplayCard.OnCardSelected -= OnCardSelected;
        StateDisplayCard.OnCardHeld -= OnStateHeld;

        stateCardsUIParent.gameObject.SetActive(false);
    }

    private void OnStateHeld(StateCard chosen)
    {
        // lock UI immediately so user can't double tap
        HandleCardHeld();

        // forward the actual choice to logic
        //TODO: bus
        // _eventManager.HandleStateChosen(chosen);
    }

    private void ShowStateCards(Player attacker, List<StateCard> statesToDisplay)
    {
        EnqueueUI(ShowStateCardsRoutine(attacker, statesToDisplay));
    }

    private IEnumerator ShowStateCardsRoutine(Player attacker, List<StateCard> statesToDisplay)
    {
        _currentPlayer = attacker;
        
        statesScreen.SetActive(true);

        StateDisplayCard.OnCardSelected += OnCardSelected;
        StateDisplayCard.OnCardHeld += OnStateHeld;

        CreateChallengeStatesUI(statesToDisplay, spacingBetweenStateCards);

        yield return AnimateEventUIRoutine();

        rollDiceButton.interactable = !AIManager.Instance.IsAIPlayer(_currentPlayer);
    }

    private void OnCardSelected(ISelectableDisplayCard card)
    {
        var newHighlightedCard = card as StateDisplayCard;
        if (!newHighlightedCard) return;
        
        if (!_highlightedCard)
            _highlightedCard = newHighlightedCard;

        if (_highlightedCard == newHighlightedCard) return;
        
        _highlightedCard.SetIsSelected(false);
        _highlightedCard = newHighlightedCard;
        _highlightedCard.SetIsSelected(true);
    }

    private void ShowDuel(Player attacker, Player defender, Card chosenCard)
    {
        EnqueueUI(ShowDuelRoutine(attacker, defender, chosenCard));
    }

    private IEnumerator ShowDuelRoutine(Player attacker, Player defender, Card chosenCard)
    {
        _currentPlayer = attacker;

        StateDisplayCard.OnCardSelected -= OnCardSelected;
        StateDisplayCard.OnCardHeld -= OnStateHeld;
        
        statesScreen.SetActive(false);
        duelScreen.SetActive(true);

        // attacker card
        CreateCardInTransform<PlayerDisplayCard>(attacker.PlayerDisplayCard.gameObject, leftCardUI, attacker.assignedActor);

        // defender card
        if (defender)
            CreateCardInTransform<PlayerDisplayCard>(defender.PlayerDisplayCard.gameObject, rightCardUI, defender.assignedActor);

        // mid card
        switch (chosenCard)
        {
            case StateCard s:
                CreateCardInTransform<StateDisplayCard>(_mainUI.stateCardPrefab, midCardUI, s);
                break;
            case InstitutionCard inst:
                CreateCardInTransform<InstitutionDisplayCard>(_mainUI.institutionCardPrefab, midCardUI, inst);
                break;
        }

        yield return AnimateEventUIRoutine();
        
        rollDiceButton.onClick.RemoveAllListeners();
        rollDiceButton.onClick.AddListener(OnRollDiceClicked);

        rollDiceButton.interactable = !AIManager.Instance.IsAIPlayer(_currentPlayer);
    }
    
    private void CreateChallengeStatesUI(List<StateCard> statesToDisplay, float spacing, float verticalSpacing = 40f)
    {
        int count = statesToDisplay.Count;
        if (count == 0) return;

        foreach (Transform child in stateCardsUIParent)
            Destroy(child.gameObject);

        List<RectTransform> cardRects = new();

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

            if (uiInstance.TryGetComponent(out RectTransform rt))
                cardRects.Add(rt);
        }

        Canvas.ForceUpdateCanvases();

        if (cardRects.Count == 0)
        {
            Debug.LogWarning("No valid card rects found.");
            return;
        }

        RectTransform parentRT   = stateCardsUIParent.GetComponent<RectTransform>();
        float parentWidth        = parentRT.rect.width;
        float parentHeight       = parentRT.rect.height;
        float cardWidth          = cardRects[0].rect.width;
        float cardHeight         = cardRects[0].rect.height;

        int cardsPerRow = Mathf.Max(1, Mathf.FloorToInt((parentWidth + spacing) / (cardWidth + spacing)));
        int rowCount    = Mathf.CeilToInt((float)count / cardsPerRow);

        float totalGridHeight = rowCount * cardHeight + (rowCount - 1) * verticalSpacing;
        float widthScale      = Mathf.Min(1f, parentWidth  / ((cardWidth + spacing) * cardsPerRow - spacing));
        float heightScale     = Mathf.Min(1f, parentHeight / totalGridHeight);
        float scaleFactor     = Mathf.Min(widthScale, heightScale);

        foreach (var rt in cardRects)
            rt.localScale = Vector3.one * scaleFactor;

        float scaledCardWidth  = cardWidth * scaleFactor;
        float scaledCardHeight = cardHeight * scaleFactor;
        float scaledHSpacing   = spacing * scaleFactor;
        float scaledVSpacing   = verticalSpacing * scaleFactor;

        float totalHeight = rowCount * scaledCardHeight + (rowCount - 1) * scaledVSpacing;
        float startY      = totalHeight / 2f - scaledCardHeight / 2f;

        int cardIndex = 0;
        for (int row = 0; row < rowCount; row++)
        {
            int cardsInRow = Mathf.Min(cardsPerRow, count - cardIndex);
            float rowWidth = cardsInRow * scaledCardWidth + (cardsInRow - 1) * scaledHSpacing;
            float startX   = -rowWidth / 2f + scaledCardWidth / 2f;

            for (int col = 0; col < cardsInRow; col++)
            {
                if (cardIndex >= cardRects.Count) break;

                RectTransform rt = cardRects[cardIndex];
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);

                float xPos = startX + col * (scaledCardWidth + scaledHSpacing);
                float yPos = startY - row * (scaledCardHeight + scaledVSpacing);

                rt.anchoredPosition = new Vector2(xPos, yPos);
                cardIndex++;
            }
        }
    }
    #endregion
    
    private void CreateCardInTransform<T>(GameObject prefab, Transform uiParent, Card cardToSet)
        where T : MonoBehaviour, IDisplayCard
    {
        foreach (Transform child in uiParent)
            Destroy(child.gameObject);

        if (cardToSet == null) return;
        
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
    }
    
    private void ReturnToMainPhaseUI()
    {
        rollDiceButton.onClick.RemoveAllListeners();
        
        EnqueueUI(ReturnToMainPhaseUIRoutine());
    }

    private IEnumerator ReturnToMainPhaseUIRoutine()
    {
        // Fade out event screen if needed
        yield return AnimateFadeOutEventScreen();
        
        _currentPlayer = null;
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

        canvasGroup.alpha = 1f;
        eventScreen.transform.localScale = Vector3.one;
    }
    
    private IEnumerator AnimateFadeInEventScreen()
    {
        bool done = false;

        // Ensure starting state (invisible + shrunk)
        canvasGroup.alpha = 0f;
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
