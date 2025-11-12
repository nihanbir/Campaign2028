
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>
/// UI for Challenge/AltStates/Duel. Now reacts primarily to GameEventBus.
/// Keeps existing animations and visuals intact.
/// </summary>
public class EUM_ChallengeEvent : MonoBehaviour
{
    [SerializeField] private Transform stateCardsUIParent;
    [SerializeField] private float spacingBetweenStateCards = 150f;
    
    [Header("Duel")]
    [SerializeField] private GameObject duelScreen;
    [SerializeField] private Transform rightCardUI;
    [SerializeField] private Transform leftCardUI;
    [SerializeField] private Transform midCardUI;

    private StateDisplayCard _highlightedCard;
    
    [SerializeField] protected GameObject eventScreen;
    [SerializeField] protected Button rollDiceButton;
    [SerializeField] protected Image diceImage;

    private GM_MainPhase _mainPhase;
    private UM_MainPhase _mainUI;
    private EventManager _eventManager;

    private Player _currentPlayer;

    private void Awake()
    {
        eventScreen.SetActive(false);
    }

    public void Initialize()
    {
        _mainPhase    = GameManager.Instance?.mainPhase;
        if (_mainPhase == null)
        {
            Debug.LogError("MainPhaseGameManager not found. Ensure it's initialized before UI.");
            return;
        }

        _mainUI       = GameUIManager.Instance.mainUI;
        _eventManager = _mainPhase.EventManager;

        GameEventBus.Instance.OnEvent += HandleGameEvent;
        
        if (rollDiceButton)
        {
            rollDiceButton.onClick.RemoveAllListeners();
            rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        }
    }

    private void HandleGameEvent(GameEvent e)
    {
        switch (e.stage)
        {
            case EventStage.ChallengeStateShown:
            {
                var payload = (ChallengeStatesData)e.Payload;
                ShowStateCards(payload.Player, payload.States);
                break;
            }

            case EventStage.DuelStarted:
            {
                var duel = (DuelData)e.Payload;
                ShowDuel(duel.Attacker, duel.Defender, duel.ChosenCard);
                break;
            }

            case EventStage.EventCompleted:
            {
                ReturnToMainPhaseUI();
                break;
            }

            case EventStage.AltStatesShown:
            {
                var p = (AltStatesData)e.Payload;
                ShowAltStates(p.Player, p.State1, p.State2);
                break;
            }

            case EventStage.PlayerRolled:
            {
                var p = (PlayerRolledData)e.Payload;
                OnPlayerRolled(p.Player, p.Roll);
                break;
            }
        }
    }

    private void AnimateEventUI(out Sequence seq)
    {
        seq = null;
        CanvasGroup cg = eventScreen.GetComponent<CanvasGroup>();
        if (!cg) cg = eventScreen.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        eventScreen.transform.localScale = Vector3.one * 0.85f;

        seq = DOTween.Sequence();
        seq.Append(cg.DOFade(1f, 0.4f).SetEase(Ease.OutCubic));
        seq.Join(eventScreen.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
    }

    private void OnRollDiceClicked()
    {
        // 🔹 Instead of calling EventManager directly, announce intent on the bus
        GameEventBus.Instance.Raise(
            new GameEvent(EventStage.RollDiceRequest, new RollDiceRequest())
        );
    }

    private void OnPlayerRolled(Player player, int roll)
    {
        GameUIManager.Instance.SetDiceSprite(diceImage);
        
        player.PlayerDisplayCard.SetRolledDiceImage();

        diceImage.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.8f);
    }
    

    #region AltStates
    private void ShowAltStates(Player player, StateCard card1, StateCard card2)
    {
        _currentPlayer = player;
        
        CreateCardInTransform<PlayerDisplayCard>(player.PlayerDisplayCard.gameObject, midCardUI, player.assignedActor);
        CreateCardInTransform<StateDisplayCard>(_mainUI.stateCardPrefab, leftCardUI,  card1);
        CreateCardInTransform<StateDisplayCard>(_mainUI.stateCardPrefab, rightCardUI, card2);
        
        //TODO: find a way to do this shi
        _mainUI.gameObject.SetActive(false);
        
        eventScreen.SetActive(true);
        
        stateCardsUIParent.gameObject.SetActive(false);
        
        duelScreen.gameObject.SetActive(true);
        
        AnimateEventUI(out var anim);

        if (anim != null)
        {
            anim.OnComplete(() =>
            {
                // Signal UI finished animating
                // GameEventBus.Instance.Raise(new GameEvent(EventStage.ClientAnimationCompleted, null));
            });
        }
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
        _eventManager.HandleStateChosen(chosen);
    }

    private void ShowStateCards(Player attacker, List<StateCard> statesToDisplay)
    {
        _currentPlayer = attacker;
        
        _mainUI.gameObject.SetActive(false);
        
        eventScreen.gameObject.SetActive(true);
        
        duelScreen.SetActive(false);
        
        stateCardsUIParent.gameObject.SetActive(true);

        StateDisplayCard.OnCardSelected += OnCardSelected;
        StateDisplayCard.OnCardHeld     += OnStateHeld;

        CreateChallengeStatesUI(statesToDisplay, spacingBetweenStateCards);

        AnimateEventUI(out var anim);
        if (anim != null)
        {
            anim.OnComplete(() =>
            {
                rollDiceButton.interactable = !AIManager.Instance.IsAIPlayer(_currentPlayer);
            });
        }
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
        StateDisplayCard.OnCardSelected -= OnCardSelected;
        StateDisplayCard.OnCardHeld -= OnStateHeld;
        
        _mainUI.gameObject.SetActive(false);
        
        eventScreen.gameObject.SetActive(true);
        
        stateCardsUIParent.gameObject.SetActive(false);
        
        duelScreen.SetActive(true);

        _currentPlayer = attacker;

        // Left: attacker
        CreateCardInTransform<PlayerDisplayCard>(_currentPlayer.PlayerDisplayCard.gameObject, leftCardUI, _currentPlayer.assignedActor);

        // Right: defender
        if (defender)
            CreateCardInTransform<PlayerDisplayCard>(defender.PlayerDisplayCard.gameObject, rightCardUI, defender.assignedActor);

        // Mid: chosen card
        switch (chosenCard)
        {
            case StateCard stateCard:
                CreateCardInTransform<StateDisplayCard>(_mainUI.stateCardPrefab, midCardUI, stateCard);
                break;
            case InstitutionCard instCard:
                CreateCardInTransform<InstitutionDisplayCard>(_mainUI.institutionCardPrefab, midCardUI, instCard);
                break;
        }
        
        AnimateEventUI(out var anim);

        if (anim != null)
        {
            anim.OnComplete(() =>
            {
                // Tell everyone (especially AI) that visuals are done
                // GameEventBus.Instance.Raise(new GameEvent(EventStage.ClientAnimationCompleted, null));

                rollDiceButton.interactable = !AIManager.Instance.IsAIPlayer(_currentPlayer);
            });
        }
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

    private void ReturnToMainPhaseUI()
    {
        //TODO: animate then continue
        
        _mainUI.gameObject.SetActive(true);
        eventScreen.SetActive(false);

        _currentPlayer  = null;
        _highlightedCard = null;

        duelScreen.SetActive(false);
        stateCardsUIParent.gameObject.SetActive(false);
    }

    private void CreateCardInTransform<T>(GameObject prefab, Transform uiParent, Card cardToSet)
        where T : MonoBehaviour, IDisplayCard
    {
        foreach (Transform child in uiParent)
            Destroy(child.gameObject);

        if (cardToSet == null) return;
        
        var cardGO = Instantiate(prefab, uiParent);
        cardGO.SetActive(true);

        if (cardGO.TryGetComponent(out T displayCard))
        {
            displayCard.SetCard(cardToSet);
        }
        else
        {
            Debug.LogError($"{prefab.name} is missing {typeof(T).Name} component.");
        }

        if (cardGO.TryGetComponent(out RectTransform rt))
            rt.anchoredPosition = Vector2.zero;
    }
}
