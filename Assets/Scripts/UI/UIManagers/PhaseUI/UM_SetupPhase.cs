
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UM_SetupPhase : UM_BasePhase
{
    public override GamePhase PhaseType => GamePhase.Setup;
    
    [Header("Card Display")]
    public GameObject cardDisplayPrefab;
    public Transform playerUIParent;
    public Transform actorUIParent;
    public float spacingBetweenPlayerCards = 150f;
    public float spacingBetweenActorCards = 300f;
    
    private PlayerDisplayCard _highlightedCard;
    private List<PlayerDisplayCard> _playerDisplayCards = new();
    
    private PlayerDisplayCard _selectedActorCard;
    private CanvasGroup _canvasGroup;
    
    private GM_SetupPhase _setupPhase;
    
    #region Initialize Phase UI

    protected override void OnPhaseEnabled()
    {
        _setupPhase = game.GetCurrentPhaseAs<GM_SetupPhase>();
        
        // _phase.OnPlayerTurnStarted +=
        if (!_canvasGroup)
            _canvasGroup = gameObject.GetComponent<CanvasGroup>() 
                           ?? gameObject.AddComponent<CanvasGroup>();

        CreateCardUI(CardDisplayType.UnassignedActor, actorUIParent, spacingBetweenActorCards);
        CreateCardUI(CardDisplayType.UnassignedPlayer, playerUIParent, spacingBetweenPlayerCards);
        
        base.OnPhaseEnabled();
        
    }

    protected override void SubscribeToPhaseEvents()
    {
        _setupPhase.OnPlayerTurnStarted += OnPlayerTurnStarted;
        _setupPhase.OnPlayerTurnEnded += OnPlayerTurnEnded;
        _setupPhase.OnAllPlayersRolled += HideDiceResults;
        // PlayerDisplayCard.OnCardSelected += SelectActorCard;
        // game.OnActorAssignedToPlayer += AssignSelectedActorToPlayer;
    }

    private void HideDiceResults()
    {
        // StartCoroutine(WaitForVisuals());
        foreach (var card in _playerDisplayCards)
        {
            card.ShowDice(true);
        }
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
                count = _setupPhase.GetUnassignedActors().Count;
                break;
            
            case CardDisplayType.UnassignedPlayer:
                count = _setupPhase.GetUnassignedPlayers().Count;
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
                    displayCard.SetCard(_setupPhase.GetUnassignedActors()[i]);
                }
                else
                {
                    _setupPhase.GetUnassignedPlayers()[i].SetDisplayCard(displayCard);
                    _playerDisplayCards.Add(displayCard);
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
    
    #region Turn State UI

    protected override void OnPlayerTurnStarted(Player player)
    {
        base.OnPlayerTurnStarted(player);
        
        bool isAssignStage = GameManager.Instance.setupPhase.CurrentStage == SetupStage.AssignActor;
        
        // Show/hide appropriate UI elements
        rollDiceButton.gameObject.SetActive(!isAssignStage);
        
        EnableDiceButton(!isPlayerAI);
        EnableCanvasGroup(!isPlayerAI);
    }

    private void EnableCanvasGroup(bool enable)
    {
        if (!_canvasGroup) _canvasGroup = gameObject.GetComponent<CanvasGroup>();
        
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
    
}