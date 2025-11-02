
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ChallengeStateUIManager : MonoBehaviour
{
    [SerializeField] private GameObject eventScreen;
    [SerializeField] private Transform challengeUIParent;
    [SerializeField] private float spacingBetweenStateCards = 150f;
    
    [Header("Duel")]
    [SerializeField] private GameObject duelScreen;
    [SerializeField] private Transform defenderUI;
    [SerializeField] private Transform playerUI;
    [SerializeField] private Transform chosenStateUI;
    [SerializeField] private Button rollDiceButton;

    private MainPhaseGameManager _mainPhase;
    private MainPhaseUIManager _mainUI; 
    private EventManager _eventManager;
    
    private StateDisplayCard _chosenCard;

    private void Awake()
    {
        eventScreen.SetActive(false);
        duelScreen.SetActive(false);
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
        
        if (rollDiceButton)
            rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        
    }

    public void OnRollDiceClicked()
    {
        var currentPlayer = GameManager.Instance.CurrentPlayer;
        
        GameUIManager.Instance.OnRollDiceClicked(rollDiceButton);
        currentPlayer.PlayerDisplayCard.SetRolledDiceImage();

        _mainPhase.PlayerRolledDice();
    }


    private void ShowStateCards(List<StateCard> statesToDisplay)
    {
        _mainUI.gameObject.SetActive(false);
        eventScreen.SetActive(true);
        
        CreateChallengeStatesUI(statesToDisplay, spacingBetweenStateCards);
    }

    private void ReturnToMainPhaseUI()
    {
        StateDisplayCard.OnCardHeld -= HandleCardHeld;
        _mainUI.gameObject.SetActive(true);
        eventScreen.SetActive(false);
        duelScreen.SetActive(false);
        
    }

    private void ShowDuel(Player defender, StateCard chosenState)
    {
        duelScreen.SetActive(true);
        
        // Spawn current player
        var currentPlayer = GameManager.Instance.CurrentPlayer;
        CreateCardInTransform<PlayerDisplayCard>(currentPlayer.PlayerDisplayCard.gameObject, playerUI, currentPlayer.assignedActor);
        
        // Spawn defender player
        CreateCardInTransform<PlayerDisplayCard>(defender.PlayerDisplayCard.gameObject, playerUI, defender.assignedActor);
        
        // Spawn chosen state
        CreateCardInTransform<StateDisplayCard>(_mainUI.stateCardPrefab, chosenStateUI, chosenState);
        
        Debug.Log($"[ChallengeUI] Showing duel between attacker {currentPlayer.playerID} and defender {defender.playerID} for state {chosenState.cardName}");
        
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
    }


    private void CreateChallengeStatesUI(List<StateCard> statesToDisplay, float spacing, float verticalSpacing = 40f)
    {
        StateDisplayCard.OnCardHeld += HandleCardHeld;
        
        int count = statesToDisplay.Count;
        if (count == 0) return;

        // Clear previous children
        foreach (Transform child in challengeUIParent)
            Destroy(child.gameObject);

        List<RectTransform> cardRects = new();

        // 1️⃣ Instantiate all cards
        for (int i = 0; i < count; i++)
        {
            GameObject uiInstance = Instantiate(_mainUI.stateCardPrefab, challengeUIParent);
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

        RectTransform parentRT = challengeUIParent.GetComponent<RectTransform>();
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

    private void HandleCardHeld(StateDisplayCard card)
    {
        // Prevent double selection
        foreach (Transform child in challengeUIParent)
            if (child.TryGetComponent(out StateDisplayCard display))
                display.SetClickable(false);
        
        _chosenCard = card;
        StateCard chosenState = card.GetCard();
        Debug.Log($"[ChallengeUI] Player held {chosenState.cardName} → set as challenge target.");

        Player defender = _mainPhase.GetHeldStates().FirstOrDefault(player => player.Value == chosenState).Key;
        ShowDuel(defender, chosenState);
    }
}
