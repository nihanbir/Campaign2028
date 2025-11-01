
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChallengeStateUIManager : MonoBehaviour
{
    [SerializeField] private GameObject eventScreen;
    [SerializeField] private Transform challengeUIParent;
    [SerializeField] private Button rollDiceButton;
    [SerializeField] private float spacingBetweenStateCards = 150f;

    private MainPhaseGameManager _mainPhase;
    private MainPhaseUIManager _mainUI; 
    private EventManager _eventManager;
    
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
        _eventManager.OnChallengeState += OnChallengeState;
        
        eventScreen.SetActive(false);
    }
    
    private void OnChallengeState(List<StateCard> statesToDisplay)
    {
        _mainUI.gameObject.SetActive(false);
        eventScreen.SetActive(true);
        rollDiceButton.gameObject.SetActive(false);
        
        CreateChallengeStatesUI(statesToDisplay, spacingBetweenStateCards);
        // ShowChallengeSelection(heldStates, selectedState =>
        // {
        //     Player defender = heldStates[selectedState];
        //     ShowChallengeDuel(player, defender, selectedState, () =>
        //     {
        //         int roll = GameUIManager.Instance.DiceRoll;
        //         bool success = selectedState.IsSuccessfulRoll(roll, player.assignedActor.team);
        //
        //         if (success)
        //         {
        //             Debug.Log($"Player {player.playerID} wins challenge and takes {selectedState.cardName}!");
        //             defender.RemoveState(selectedState);
        //             player.CaptureCard(selectedState);
        //             _game.UpdateStateOwnership(selectedState, player);
        //         }
        //         else
        //         {
        //             Debug.Log($"Player {player.playerID} failed challenge for {selectedState.cardName}.");
        //             if (card.canReturnToDeck)
        //                 _game.ReturnCardToDeck(card);
        //         }
        //
        //         ReturnToMainPhaseUI();
        //     });
        // });
    }

    private void CreateChallengeStatesUI(List<StateCard> statesToDisplay, float spacing, float verticalSpacing = 40f)
    {
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
}
