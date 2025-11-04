
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChallengeStateUIManager : MonoBehaviour
{
    [SerializeField] private GameObject eventScreen;
    [SerializeField] private Transform stateCardsUIParent;
    [SerializeField] private float spacingBetweenStateCards = 150f;
    
    [Header("Duel")]
    [SerializeField] private GameObject duelScreen;
    [SerializeField] private Transform defenderUI;
    [SerializeField] private Transform attackerUI;
    [SerializeField] private Transform chosenCardUI;
    [SerializeField] private Button rollDiceButton;

    private MainPhaseGameManager _mainPhase;
    private MainPhaseUIManager _mainUI; 
    private EventManager _eventManager;
    private StateDisplayCard _highlightedCard;

    private Player _attacker;
    
    private void Awake()
    {
        eventScreen.SetActive(false);
    }

    public void InitializeManager()
    {
        _mainPhase = GameManager.Instance?.mainPhase;

        if (_mainPhase == null)
        {
            Debug.LogError("MainPhaseGameManager not found. Ensure it's initialized before UI.");
            return;
        }
        _mainUI = GameUIManager.Instance.mainUI;
        _eventManager = _mainPhase.EventManager;
        _eventManager.OnChallengeState += ShowStateCards;
        _eventManager.OnDuelActive += ShowDuel;
        _eventManager.OnDuelCompleted += ReturnToMainPhaseUI;
        
        if (rollDiceButton)
            rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        
    }

    public void OnRollDiceClicked()
    {
        GameUIManager.Instance.OnRollDiceClicked(rollDiceButton);
        _attacker.PlayerDisplayCard.SetRolledDiceImage();
        
        int roll = GameUIManager.Instance.DiceRoll;
        _eventManager.EvaluateCapture(roll);
    }

    private void HandleCardHeld()
    {
        // Prevent double selection
        foreach (Transform child in stateCardsUIParent)
            if (child.TryGetComponent(out StateDisplayCard display))
                display.SetClickable(false);
        
        StateDisplayCard.OnCardHighlighted -= OnCardHighlighted;
        StateDisplayCard.OnCardHeld -= _ => HandleCardHeld();
        
        stateCardsUIParent.gameObject.SetActive(false);
    }
    
    private void ShowStateCards(List<StateCard> statesToDisplay)
    {
        _mainUI.gameObject.SetActive(false);
        eventScreen.SetActive(true);
        duelScreen.SetActive(false);
        stateCardsUIParent.gameObject.SetActive(true);
        
        StateDisplayCard.OnCardHighlighted += OnCardHighlighted;
        StateDisplayCard.OnCardHeld += _ => HandleCardHeld();
        
        CreateChallengeStatesUI(statesToDisplay, spacingBetweenStateCards);
    }

    private void OnCardHighlighted(StateDisplayCard newHighlightedCard)
    {
        if (!_highlightedCard)
        {
            _highlightedCard = newHighlightedCard;
        }

        if (_highlightedCard == newHighlightedCard) return;
        
        _highlightedCard.SetIsSelected(false);
        
        _highlightedCard = newHighlightedCard;
        _highlightedCard.SetIsSelected(true);

    }

    private void ShowDuel(Player defender, Card chosenCard)
    {
        
        
        _attacker = GameManager.Instance.CurrentPlayer;
        
        if (AIManager.Instance.IsAIPlayer(_attacker))
        {
            rollDiceButton.interactable = false;
        }
        
        // Spawn current player
        CreateCardInTransform<PlayerDisplayCard>(_attacker.PlayerDisplayCard.gameObject, attackerUI, _attacker.assignedActor);
        
        // Spawn defender player
        CreateCardInTransform<PlayerDisplayCard>(defender.PlayerDisplayCard.gameObject, defenderUI, defender.assignedActor);
        
        // Spawn chosen card
        switch (chosenCard)
        {
            case StateCard stateCard:
                
                CreateCardInTransform<StateDisplayCard>(_mainUI.stateCardPrefab, chosenCardUI, stateCard);
                break;
            
            case InstitutionCard institutionCard:
                
                CreateCardInTransform<InstitutionDisplayCard>(_mainUI.institutionCardPrefab, chosenCardUI, institutionCard);
                break;
        }
        
        Debug.Log($"[ChallengeUI] Showing duel between attacker {_attacker.playerID} and defender {defender.playerID} for state {chosenCard.cardName}");
        
        duelScreen.SetActive(true);
        
    }

    private void CreateCardInTransform<T>(GameObject prefab, Transform uiParent, Card cardToSet)
        where T : MonoBehaviour, IDisplayCard
    {
        // Clear previous card(s)
        foreach (Transform child in uiParent)
            Destroy(child.gameObject);

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
        
        if (displayCard.TryGetComponent(out RectTransform rt))
        {
            rt.anchoredPosition = Vector2.zero;
        }
    }
    
    private void CreateChallengeStatesUI(List<StateCard> statesToDisplay, float spacing, float verticalSpacing = 40f)
    {
        
        int count = statesToDisplay.Count;
        Debug.Log($"available states passed to UI: {statesToDisplay.Count}");
        if (count == 0) return;

        // Clear previous children
        foreach (Transform child in stateCardsUIParent)
            Destroy(child.gameObject);

        List<RectTransform> cardRects = new();

        // 1️⃣ Instantiate all cards
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

        RectTransform parentRT = stateCardsUIParent.GetComponent<RectTransform>();
        float parentWidth = parentRT.rect.width;
        float parentHeight = parentRT.rect.height;
        float cardWidth = cardRects[0].rect.width;
        float cardHeight = cardRects[0].rect.height;

        // 2️⃣ Compute grid size
        int cardsPerRow = Mathf.Max(1, Mathf.FloorToInt((parentWidth + spacing) / (cardWidth + spacing)));
        int rowCount = Mathf.CeilToInt((float)count / cardsPerRow);

        // 3️⃣ Auto-shrink if needed
        float totalGridHeight = rowCount * cardHeight + (rowCount - 1) * verticalSpacing;
        float widthScale = Mathf.Min(1f, parentWidth / ((cardWidth + spacing) * cardsPerRow - spacing));
        float heightScale = Mathf.Min(1f, parentHeight / totalGridHeight);
        float scaleFactor = Mathf.Min(widthScale, heightScale);

        foreach (var rt in cardRects)
            rt.localScale = Vector3.one * scaleFactor;

        float scaledCardWidth = cardWidth * scaleFactor;
        float scaledCardHeight = cardHeight * scaleFactor;
        float scaledHSpacing = spacing * scaleFactor;
        float scaledVSpacing = verticalSpacing * scaleFactor;

        float totalHeight = rowCount * scaledCardHeight + (rowCount - 1) * scaledVSpacing;
        float startY = totalHeight / 2f - scaledCardHeight / 2f;

        // 4️⃣ Place cards row by row
        int cardIndex = 0;
        for (int row = 0; row < rowCount; row++)
        {
            // Calculate how many cards are actually in this row
            int cardsInThisRow = Mathf.Min(cardsPerRow, count - cardIndex);

            // Compute this row’s total width so we can center it
            float rowWidth = cardsInThisRow * scaledCardWidth + (cardsInThisRow - 1) * scaledHSpacing;
            float startX = -rowWidth / 2f + scaledCardWidth / 2f;

            for (int col = 0; col < cardsInThisRow; col++)
            {
                if (cardIndex >= cardRects.Count)
                    break;

                RectTransform rt = cardRects[cardIndex];
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);

                float xPos = startX + col * (scaledCardWidth + scaledHSpacing);
                float yPos = startY - row * (scaledCardHeight + scaledVSpacing);

                rt.anchoredPosition = new Vector2(xPos, yPos);

                cardIndex++;
            }
        }

        Debug.Log($"[ChallengeUI] Placed {count} state cards in {rowCount} rows × {cardsPerRow} max columns (centered rows, scale={scaleFactor:F2}).");
    }

    private void ReturnToMainPhaseUI()
    {
        NullifyVariables();
        
        ToggleGOs();
        
        _mainPhase.EndPlayerTurn();
    }

    private void NullifyVariables()
    {
        _attacker = null;
        _highlightedCard = null;
    }

    private void ToggleGOs()
    {
        _mainUI.gameObject.SetActive(true);
        eventScreen.SetActive(false);
        duelScreen.SetActive(false);
        stateCardsUIParent.gameObject.SetActive(false);
    }
    
}
