
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UM_SetupPhase : UM_BasePhase
{
    public override GamePhase PhaseType => GamePhase.Setup;
    
   [Header("Setup Phase")]     
    public GameObject setupGamePhase;
    public Button rollDiceButton;
    
    [Header("Card Display")]
    public GameObject cardDisplayPrefab;
    public Transform playerUIParent;
    public Transform actorUIParent;
    public float spacingBetweenPlayerCards = 150f;
    public float spacingBetweenActorCards = 300f;
    
    private PlayerDisplayCard _highlightedCard;
    
    private PlayerDisplayCard _selectedActorCard;
    private CanvasGroup _canvasGroup;
    
    private GM_SetupPhase _phase;
    
    #region Initialize Phase UI

    public override void OnPhaseEnabled()
    {
        Debug.Log("init setup ui");
        
        _phase = game.GetCurrentPhaseAs<GM_SetupPhase>();
        
        rollDiceButton.onClick.RemoveAllListeners();
        rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        
        if (!_canvasGroup)
            _canvasGroup = setupGamePhase.GetComponent<CanvasGroup>() 
                           ?? setupGamePhase.AddComponent<CanvasGroup>();

        CreateCardUI(CardDisplayType.UnassignedActor, actorUIParent, spacingBetweenActorCards);
        CreateCardUI(CardDisplayType.UnassignedPlayer, playerUIParent, spacingBetweenPlayerCards);
        
        // PlayerDisplayCard.OnCardSelected += SelectActorCard;
        // game.OnActorAssignedToPlayer += AssignSelectedActorToPlayer;
        
        base.OnPhaseEnabled();
        
    }
    
    void CreateCardUI(CardDisplayType cardType, Transform parent, float spacing)
    {
        if (!GameManager.Instance)
        {
            Debug.LogError("GameManager instance is not set");
            return;
        }

        int count;
        
        switch (cardType)
        {
            case CardDisplayType.UnassignedActor:
                count = _phase.GetUnassignedActors().Count;
                break;
            
            case CardDisplayType.UnassignedPlayer:
                count = _phase.GetUnassignedPlayers().Count;
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
                    displayCard.SetCard(_phase.GetUnassignedActors()[i]);
                }
                else
                {
                    _phase.GetUnassignedPlayers()[i].SetDisplayCard(displayCard);
                }
                
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
        currentPlayer.PlayerDisplayCard.SetRolledDiceImage();
        
        GameManager.Instance.StartCoroutine(WaitForVisuals());
        
    }
    
    private IEnumerator WaitForVisuals()
    {
        yield return new WaitForSeconds(0.5f); // small delay ensures rolled dices being visible
        GameManager.Instance.setupPhase.PlayerRolledDice();
    }
    
    #endregion

    #region Turn State UI

    public void OnPlayerTurnStarted(Player currentPlayer)
    {
        bool isAssignStage = GameManager.Instance.setupPhase.CurrentStage == SetupStage.AssignActor;
        
        // Show/hide appropriate UI elements
        rollDiceButton.gameObject.SetActive(!isAssignStage);
        
        // Disable UI for AI players
        bool isAI = AIManager.Instance.IsAIPlayer(currentPlayer);
        EnableCanvasGroup(!isAI);
        
        currentPlayer.PlayerDisplayCard.Highlight();
        
    }

    public void OnplayerTurnEnded(Player previousPlayer)
    {
        previousPlayer.PlayerDisplayCard.RemoveHighlight();
    }

    private void EnableCanvasGroup(bool enable)
    {
        if (!_canvasGroup) _canvasGroup = setupGamePhase.GetComponent<CanvasGroup>();
        
        _canvasGroup.interactable = enable;
        _canvasGroup.blocksRaycasts = enable;
    }

    #endregion

    #region Actor Assignment UI
    
    // public void SelectActorCard(ISelectableDisplayCard card)
    // {
    //     var actorCard = card as PlayerDisplayCard;
    //     if (!actorCard)
    //     {
    //         Debug.Log($"no actor");
    //         return;
    //     }
    //     
    //     if (_selectedActorCard == actorCard)
    //         return;
    //
    //     if (_selectedActorCard)
    //         _selectedActorCard.SetIsSelected(false);
    //
    //     _selectedActorCard = actorCard;
    //     _selectedActorCard?.SetIsSelected(true);
    //
    //     Debug.Log($"Selected actor: {_selectedActorCard.GetCard().cardName}");
    // }
    //
    // public void AssignSelectedActorToPlayer(PlayerDisplayCard playerCard)
    // {
    //     if (_selectedActorCard == null)
    //     {
    //         Debug.LogWarning("No actor card selected to assign.");
    //         return;
    //     }
    //
    //     ActorCard actorToAssign = _selectedActorCard.GetCard();
    //     
    //     // Update UI
    //     RemoveCard(_selectedActorCard);
    //     playerCard.ConvertToAssignedActor(actorToAssign);
    //     
    //     _selectedActorCard = null;
    // }
    //
    // public void AutoAssignLastActor(Player lastPlayer, ActorCard lastActor)
    // { 
    //     Debug.Log($"Auto-assigning last actor {lastActor.cardName} to Player {lastPlayer.playerID}");
    //
    //     var lastActorCard = FindDisplayCardForUnassignedActor(lastActor);
    //     var lastPlayerCard = FindDisplayCardForPlayer(lastPlayer);
    //     // Update UI
    //     RemoveCard(lastActorCard);
    //     lastPlayerCard.ConvertToAssignedActor(lastActor);
    // }
    //
    // private void RemoveCard(PlayerDisplayCard card)
    // {
    //     Destroy(card.gameObject);
    // }
    //
    // private PlayerDisplayCard FindDisplayCardForPlayer(Player player)
    // {
    //     foreach (var playerDisplay in playerUIParent)
    //     {
    //         var display = playerDisplay as PlayerDisplayCard;
    //         if (!display) return null;
    //         
    //         if (display.owningPlayer == player)
    //             return display;
    //     }
    //
    //     return null;
    // }
    //
    // private PlayerDisplayCard FindDisplayCardForUnassignedActor(ActorCard actor)
    // {
    //     foreach (var actorDisplay in actorUIParent)
    //     {
    //         var display = actorDisplay as PlayerDisplayCard;
    //         if (!display) return null;
    //         
    //         if (display.GetCard() == actor)
    //             return display;
    //     }
    //
    //     return null;
    // }

    #endregion

    #region Cleanup

    public void OnSetupPhaseComplete()
    {
        Debug.Log("Setup phase UI cleanup");
        setupGamePhase.SetActive(false);
        // Additional cleanup if needed
    }

    #endregion
}