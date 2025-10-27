using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SetupPhaseUIManager : MonoBehaviour
{
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
    
    private PlayerDisplayCard _selectedActorCard;
    private CanvasGroup _canvasGroup;

    #region Initialize Phase UI

    public void InitializePhaseUI()
    {
        
        Debug.Log("init setup ui");
        rollDiceButton.onClick.RemoveAllListeners();
        rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        
        if (!_canvasGroup)
            _canvasGroup = setupGamephase.GetComponent<CanvasGroup>() 
                           ?? setupGamephase.AddComponent<CanvasGroup>();

        CreateCardUI(CardDisplayType.UnassignedActor, actorUIParent, spacingBetweenActorCards);
        CreateCardUI(CardDisplayType.UnassignedPlayer, playerUIParent, spacingBetweenPlayerCards);
        
        actorUIParent.gameObject.SetActive(false);
    }
    
    void CreateCardUI(CardDisplayType cardType, Transform parent, float spacing)
    {
        if (!GameManager.Instance)
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
                count = GameManager.Instance.actorDeck.Count;
                break;
            case CardDisplayType.UnassignedPlayer:
                targetList = unassignedPlayerCards;
                count = GameManager.Instance.players.Count;
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
                    displayCard.SetActor(GameManager.Instance.actorDeck[i]);
                }
                else if (cardType == CardDisplayType.UnassignedPlayer)
                {
                    GameManager.Instance.players[i].SetDisplayCard(displayCard);
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
        var currentPlayer = GameManager.Instance.CurrentPlayer;
        
        GameUIManager.Instance.OnRollDiceClicked(rollDiceButton);
        currentPlayer.playerDisplayCard.SetRolledDiceImage();
        
        GameManager.Instance.setupPhase.PlayerRolledDice();
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
        bool isAssignStage = GameManager.Instance.setupPhase.CurrentStage == SetupStage.AssignActor;
        
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
        if (!_canvasGroup) _canvasGroup = setupGamephase.GetComponent<CanvasGroup>();
        
        _canvasGroup.interactable = enable;
        _canvasGroup.blocksRaycasts = enable;
    }

    #endregion

    #region Actor Assignment UI

    public void SelectActorCard(PlayerDisplayCard actorCard)
    {
        // Deselect previous card if any
        if (_selectedActorCard != null)
        {
            // TODO: Remove visual highlight from previous selection
        }

        _selectedActorCard = actorCard;
        Debug.Log($"Selected actor: {_selectedActorCard.GetActorCard().cardName}");
        
        // TODO: Add visual highlight for selected card
    }
    
    public void AssignSelectedActorToPlayer(Player targetPlayer, PlayerDisplayCard playerCard)
    {
        if (_selectedActorCard == null)
        {
            Debug.LogWarning("No actor card selected to assign.");
            return;
        }

        ActorCard actorToAssign = _selectedActorCard.GetActorCard();
        
        // Update UI
        RemoveCard(_selectedActorCard, unassignedActorCards);
        playerCard.ConvertToAssignedActor(actorToAssign);
        unassignedPlayerCards.Remove(playerCard);
        
        _selectedActorCard = null;
        
        // Let the game manager handle the logic and validation
        GameManager.Instance.setupPhase.AssignActorToPlayer(targetPlayer, actorToAssign);
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