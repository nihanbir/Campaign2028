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
    public GameObject cardDisplayPrefab; // Single unified prefab
    public Transform playerUIParent;
    public Transform actorUIParent;
    public float spacingBetweenPlayerCards = 150f;
    public float spacingBetweenActorCards = 300f;
    
    [HideInInspector] public List<PlayerDisplayCard> unassignedPlayerCards = new List<PlayerDisplayCard>();
    [HideInInspector] public List<PlayerDisplayCard> unassignedActorCards = new List<PlayerDisplayCard>();
    
    private int assignedActorCardCount;
    private PlayerDisplayCard selectedActorCard;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void EnableCanvasGroup(bool enable)
    {
        if (!canvasGroup) canvasGroup = setupGamephase.GetComponent<CanvasGroup>();
        
        canvasGroup.interactable = enable;
        canvasGroup.blocksRaycasts = enable;
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
        
        // Determine what we're creating based on card type
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
                
                // Setup based on type
                if (cardType == CardDisplayType.UnassignedActor)
                {
                    displayCard.SetActor(SetupPhaseGameManager.Instance.actorDeck[i]);
                }
                else if (cardType == CardDisplayType.UnassignedPlayer)
                {
                    SetupPhaseGameManager.Instance.players[i].SetDisplayCard(displayCard);
                }
                
                targetList.Add(displayCard);
                
                // Position card
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
    
    public void OnRollDiceClicked()
    {
        var currentPlayer = SetupPhaseGameManager.Instance.CurrentPlayer;
        
        GameUIManager.Instance.OnRollDiceClicked(rollDiceButton);
        currentPlayer.playerDisplayCard.SetRolledDiceImage();
        
        SetupPhaseGameManager.Instance.PlayerRolledDice();
    }
    
    public void OnPlayerTurnStarted(Player currentPlayer)
    {
        bool isAssignStage = SetupPhaseGameManager.Instance.CurrentStage == SetupStage.AssignActor;
        
        actorUIParent.gameObject.SetActive(isAssignStage);
        rollDiceButton.gameObject.SetActive(!isAssignStage);
        
        bool isAI = SetupPhaseAIManager.Instance.IsAIPlayer(currentPlayer);
        EnableCanvasGroup(!isAI);
        
        currentPlayer.playerDisplayCard.Highlight();
        
        Debug.Log($"{(isAI ? "AI" : "Human")} Player {currentPlayer.playerID} turn started");
    }

    public void OnplayerTurnEnded(Player previousPlayer)
    {
        previousPlayer.playerDisplayCard.RemoveHighlight();
    }
    
    public void SelectActorCard(PlayerDisplayCard actorCard)
    {
        selectedActorCard = actorCard;
        Debug.Log($"Selected actor: {selectedActorCard.GetActorCard().cardName}");
        // TODO: Add visual highlight for selected card
    }
    
    public void AssignSelectedActorToPlayer(Player player, PlayerDisplayCard playerCard)
    {
        if (!selectedActorCard)
        {
            Debug.LogWarning("No actor card selected to assign.");
            return;
        }
        
        if (player == SetupPhaseGameManager.Instance.CurrentPlayer)
        {
            Debug.LogWarning("Can't assign an actor to yourself!");
            return;
        }
        
        ActorCard actorToAssign = selectedActorCard.GetActorCard();
        player.assignedActor = actorToAssign;
        
        Debug.Log($"Assigned {actorToAssign.cardName} to Player {player.playerID}");
        
        // Remove the unassigned actor card
        RemoveCard(selectedActorCard, unassignedActorCards);
        
        // Convert player card to assigned actor display
        playerCard.ConvertToAssignedActor(actorToAssign);
        unassignedPlayerCards.Remove(playerCard);
        
        assignedActorCardCount++;
        selectedActorCard = null;

        // Check if only one player and one actor remain
        if (unassignedPlayerCards.Count == 1 && unassignedActorCards.Count == 1)
        {
            AutoAssignLastActor();
            return;
        }

        if (assignedActorCardCount == SetupPhaseGameManager.Instance.players.Count)
        {
            OnAllActorsAssigned();
            return;
        }
        
        SetupPhaseGameManager.Instance.CurrentStage = SetupStage.Roll;
        SetupPhaseGameManager.Instance.EndTurn();
    }

    private void AutoAssignLastActor()
    {
        PlayerDisplayCard lastPlayerCard = unassignedPlayerCards[0];
        PlayerDisplayCard lastActorCard = unassignedActorCards[0];
        
        Player lastPlayer = lastPlayerCard.owningPlayer;
        ActorCard lastActor = lastActorCard.GetActorCard();
        
        Debug.Log($"Auto-assigning last actor {lastActor.cardName} to Player {lastPlayer.playerID}");
        
        lastPlayer.assignedActor = lastActor;
        
        // Remove the last actor card
        RemoveCard(lastActorCard, unassignedActorCards);
        
        // Convert last player card to assigned actor display
        lastPlayerCard.ConvertToAssignedActor(lastActor);
        unassignedPlayerCards.Remove(lastPlayerCard);
        
        assignedActorCardCount++;
        
        OnAllActorsAssigned();
    }

    private void RemoveCard(PlayerDisplayCard card, List<PlayerDisplayCard> fromList)
    {
        fromList.Remove(card);
        Destroy(card.gameObject);
    }

    public void UpdateUIForPlayer(Player player, bool hideDice)
    {
        if (player.playerDisplayCard)
        {
            player.playerDisplayCard.ShowDice(!hideDice);
        }
    }

    private void OnAllActorsAssigned()
    {
        Debug.Log("All actors assigned! Moving to Main Game Phase...");
        SetupPhaseGameManager.Instance.currentGamePhase = GamePhase.MainGame;
        GameUIManager.Instance.UpdateGamePhase(GamePhase.MainGame);
        
        // Cleanup setup phase UI
        setupGamephase.SetActive(false);
        // TODO: Initialize main game phase
    }
}