using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SetupPhaseUIManager : MonoBehaviour
{
    public static SetupPhaseUIManager Instance;
    
    [Header("Setup Phase")]     
    public GameObject setupGamephase;
    public Button rollDiceButton;
    
    [Header("Card Display")]
    public GameObject cardDisplayPrefab;
    public Transform playerUIParent;
    public Transform actorUIParent;
    public float spacingBetweenPlayerCards = 150f;
    public float spacingBetweenActorCards = 300f;
    
    [HideInInspector] public List<PlayerDisplayCard> unassignedPlayerCards = new List<PlayerDisplayCard>();
    [HideInInspector] public List<PlayerDisplayCard> unassignedActorCards = new List<PlayerDisplayCard>();
    
    private PlayerDisplayCard selectedActorCard;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    #region Initialize Phase UI

    public void InitializePhaseUI()
    {
        rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        
        canvasGroup = setupGamephase.GetComponent<CanvasGroup>();
        if (!canvasGroup) canvasGroup = setupGamephase.AddComponent<CanvasGroup>();

        CreateCardUI(CardDisplayType.UnassignedActor, actorUIParent, spacingBetweenActorCards);
        CreateCardUI(CardDisplayType.UnassignedPlayer, playerUIParent, spacingBetweenPlayerCards);
        
        actorUIParent.gameObject.SetActive(false);
    }
    
    void CreateCardUI(CardDisplayType cardType, Transform parent, float spacing)
    {
        if (!SetupPhaseGameManager.Instance)
        {
            Debug.LogError("GameManager instance is not set");
            return;
        }

        List<PlayerDisplayCard> targetList;
        int count;
        
        switch (cardType)
        {
            case CardDisplayType.UnassignedActor:
                targetList = unassignedActorCards;
                count = SetupPhaseGameManager.Instance.actorDeck.Count;
                break;
            case CardDisplayType.UnassignedPlayer:
                targetList = unassignedPlayerCards;
                count = SetupPhaseGameManager.Instance.players.Count;
                break;
            default:
                Debug.LogError($"Invalid card type for creation: {cardType}");
                return;
        }

        float totalWidth = (count - 1) * spacing;

        for (int i = 0; i < count; i++)
        {
            GameObject uiInstance = Instantiate(cardDisplayPrefab, parent);
            PlayerDisplayCard displayCard = uiInstance.GetComponent<PlayerDisplayCard>();
            
            if (displayCard)
            {
                displayCard.displayType = cardType;
                
                if (cardType == CardDisplayType.UnassignedActor)
                {
                    displayCard.SetActor(SetupPhaseGameManager.Instance.actorDeck[i]);
                }
                else if (cardType == CardDisplayType.UnassignedPlayer)
                {
                    SetupPhaseGameManager.Instance.players[i].SetDisplayCard(displayCard);
                }
                
                targetList.Add(displayCard);
                
                RectTransform rt = uiInstance.GetComponent<RectTransform>();
                if (rt)
                {
                    float xPos = i * spacing - totalWidth / 2f;
                    rt.anchoredPosition = new Vector2(xPos, 0);
                }
            }
            else
            {
                Debug.LogError("CardDisplayPrefab missing DisplayCard component.");
            }
        }
    }
    
    #endregion
    
    #region Dice Rolling UI

    public void OnRollDiceClicked()
    {
        var currentPlayer = SetupPhaseGameManager.Instance.CurrentPlayer;
        
        GameUIManager.Instance.OnRollDiceClicked(rollDiceButton);
        currentPlayer.playerDisplayCard.SetRolledDiceImage();
        
        SetupPhaseGameManager.Instance.PlayerRolledDice();
    }

    public void UpdateUIForPlayer(Player player, bool hideDice)
    {
        if (player.playerDisplayCard)
        {
            player.playerDisplayCard.ShowDice(!hideDice);
        }
    }

    #endregion

    #region Turn State UI

    public void OnPlayerTurnStarted(Player currentPlayer)
    {
        bool isAssignStage = SetupPhaseGameManager.Instance.CurrentStage == SetupStage.AssignActor;
        
        // Show/hide appropriate UI elements
        actorUIParent.gameObject.SetActive(isAssignStage);
        rollDiceButton.gameObject.SetActive(!isAssignStage);
        
        // Disable UI for AI players
        bool isAI = SetupPhaseAIManager.Instance.IsAIPlayer(currentPlayer);
        EnableCanvasGroup(!isAI);
        
        currentPlayer.playerDisplayCard.Highlight();
        
    }

    public void OnplayerTurnEnded(Player previousPlayer)
    {
        previousPlayer.playerDisplayCard.RemoveHighlight();
    }

    private void EnableCanvasGroup(bool enable)
    {
        if (!canvasGroup) canvasGroup = setupGamephase.GetComponent<CanvasGroup>();
        
        canvasGroup.interactable = enable;
        canvasGroup.blocksRaycasts = enable;
    }

    #endregion

    #region Actor Assignment UI

    public void SelectActorCard(PlayerDisplayCard actorCard)
    {
        // Deselect previous card if any
        if (selectedActorCard != null)
        {
            // TODO: Remove visual highlight from previous selection
        }

        selectedActorCard = actorCard;
        Debug.Log($"Selected actor: {selectedActorCard.GetActorCard().cardName}");
        
        // TODO: Add visual highlight for selected card
    }
    
    public void AssignSelectedActorToPlayer(Player targetPlayer, PlayerDisplayCard playerCard)
    {
        if (selectedActorCard == null)
        {
            Debug.LogWarning("No actor card selected to assign.");
            return;
        }

        ActorCard actorToAssign = selectedActorCard.GetActorCard();
        
        // Update UI
        RemoveCard(selectedActorCard, unassignedActorCards);
        playerCard.ConvertToAssignedActor(actorToAssign);
        unassignedPlayerCards.Remove(playerCard);
        
        selectedActorCard = null;
        
        // Let the game manager handle the logic and validation
        SetupPhaseGameManager.Instance.AssignActorToPlayer(targetPlayer, actorToAssign);
    }

    public void AutoAssignLastActor()
    {
        PlayerDisplayCard lastPlayerCard = unassignedPlayerCards[0];
        PlayerDisplayCard lastActorCard = unassignedActorCards[0];
        
        Player lastPlayer = lastPlayerCard.owningPlayer;
        ActorCard lastActor = lastActorCard.GetActorCard();
        
        Debug.Log($"Auto-assigning last actor {lastActor.cardName} to Player {lastPlayer.playerID}");
        
        // Let game manager handle the assignment
        lastPlayer.assignedActor = lastActor;
        
        // Update UI
        RemoveCard(lastActorCard, unassignedActorCards);
        lastPlayerCard.ConvertToAssignedActor(lastActor);
        unassignedPlayerCards.Remove(lastPlayerCard);
        
    }

    private void RemoveCard(PlayerDisplayCard card, List<PlayerDisplayCard> fromList)
    {
        fromList.Remove(card);
        Destroy(card.gameObject);
    }

    #endregion

    #region Cleanup

    public void OnSetupPhaseComplete()
    {
        Debug.Log("Setup phase UI cleanup");
        setupGamephase.SetActive(false);
        // Additional cleanup if needed
    }

    #endregion
}