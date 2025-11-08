
using UnityEngine;
using UnityEngine.UI;

public class EUM_AlternativeStates : EUM_Base
{
    [SerializeField] private Transform playerUI;
    [SerializeField] private Transform leftCardUI;
    [SerializeField] private Transform rightCardUI;

    public override void InitializeEventUI()
    {
        base.InitializeEventUI();
        
        eventManager.OnAltStatesActive += ShowUI;
        eventManager.OnAltStatesCompleted += ReturnToMainPhaseUI;
    }

    private void ShowUI(Player player, StateCard card1, StateCard card2)
    {
        currentPlayer = player;
        eventManager.activeEventUI = this;
        CreateCardInTransform<PlayerDisplayCard>(player.PlayerDisplayCard.gameObject, playerUI, player.assignedActor);
        
        CreateCardInTransform<StateDisplayCard>(mainUI.stateCardPrefab, leftCardUI, card1);
        
        CreateCardInTransform<StateDisplayCard>(mainUI.stateCardPrefab, rightCardUI, card2);
        
        eventScreen.SetActive(true);

        rollDiceButton.interactable = !AIManager.Instance.IsAIPlayer(currentPlayer);
    }
    
    public override void OnRollDiceClicked()
    {
        base.OnRollDiceClicked();
        
        eventManager.EvaluateStateDiscard(roll);
    }
}
