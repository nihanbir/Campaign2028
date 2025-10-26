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

    private void Start()
    {
        rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        
        canvasGroup = setupGamephase.GetComponent<CanvasGroup>();
        if (!canvasGroup) canvasGroup = setupGamephase.AddComponent<CanvasGroup>();

        CreateActorCardUI();
        CreateUnassignedPlayerUI();
        actorUIParent.gameObject.SetActive(false);
        
        SetupPhaseGameManager.Instance.StartTurn();
    }

    private void EnableCanvasGroup(bool enable)
    {
        if (!canvasGroup) canvasGroup = setupGamephase.GetComponent<CanvasGroup>();
        
        canvasGroup.interactable = enable;
        canvasGroup.blocksRaycasts = enable;
    }

    void CreateActorCardUI()
    {
        var actorDeck = SetupPhaseGameManager.Instance.actorDeck;
        int count = actorDeck.Count;
        float totalWidth = (count - 1) * spacingBetweenActorCards;

        for (int i = 0; i < count; i++)
        {
            GameObject uiInstance = Instantiate(cardDisplayPrefab, actorUIParent);
            PlayerDisplayCard playerDisplayCard = uiInstance.GetComponent<PlayerDisplayCard>();
            
            if (playerDisplayCard)
            {
                playerDisplayCard.displayType = CardDisplayType.UnassignedActor;
                playerDisplayCard.SetActor(actorDeck[i]);
                unassignedActorCards.Add(playerDisplayCard);
                
                RectTransform rt = uiInstance.GetComponent<RectTransform>();
                if (rt)
                {
                    float xPos = i * spacingBetweenActorCards - totalWidth / 2f;
                    rt.anchoredPosition = new Vector2(xPos, 0);
                }
            }
            else
            {
                Debug.LogError("CardDisplayPrefab missing DisplayCard component.");
            }
        }
    }
    
    void CreateUnassignedPlayerUI()
    {
        if (!SetupPhaseGameManager.Instance)
        {
            Debug.LogError("GameManager instance is not set");
            return;
        }
        
        var players = SetupPhaseGameManager.Instance.players;
        int count = players.Count;
        float totalWidth = (count - 1) * spacingBetweenPlayerCards;

        for (int i = 0; i < count; i++)
        {
            GameObject uiInstance = Instantiate(cardDisplayPrefab, playerUIParent);
            PlayerDisplayCard playerDisplayCard = uiInstance.GetComponent<PlayerDisplayCard>();
            
            if (playerDisplayCard)
            {
                playerDisplayCard.displayType = CardDisplayType.UnassignedPlayer;
                players[i].SetDisplayCard(playerDisplayCard);
                unassignedPlayerCards.Add(playerDisplayCard);
                
                RectTransform rt = uiInstance.GetComponent<RectTransform>();
                if (rt)
                {
                    float xPos = i * spacingBetweenPlayerCards - totalWidth / 2f;
                    rt.anchoredPosition = new Vector2(xPos, 0);
                }
            }
            else
            {
                Debug.LogError("CardDisplayPrefab missing DisplayCard component.");
            }
        }
    }
    
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
        
        Debug.Log($"{(isAI ? "AI" : "Human")} Player {currentPlayer.playerID} turn started - Stage: {SetupPhaseGameManager.Instance.CurrentStage}");
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

        if (assignedActorCardCount == SetupPhaseGameManager.Instance.players.Count)
        {
            OnAllActorsAssigned();
            return;
        }
        
        SetupPhaseGameManager.Instance.CurrentStage = SetupStage.Roll;
        SetupPhaseGameManager.Instance.EndTurn();
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