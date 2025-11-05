
using UnityEngine;
using UnityEngine.UI;

public class AlternativeStatesUIManager : BaseEventUI
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
        CreateCardInTransform<PlayerDisplayCard>(player.PlayerDisplayCard.gameObject, playerUI, player.assignedActor);

        if (card1 != null)
        {
            CreateCardInTransform<StateDisplayCard>(mainUI.stateCardPrefab, leftCardUI, card1);
        }
        
        if (card2 != null)
        {
            CreateCardInTransform<StateDisplayCard>(mainUI.stateCardPrefab, rightCardUI, card2);
        }
        
        eventScreen.SetActive(true);

        rollDiceButton.interactable = !AIManager.Instance.IsAIPlayer(currentPlayer);
    }
    
    public override void OnRollDiceClicked()
    {
        base.OnRollDiceClicked();
        
        eventManager.EvaluateAltStatesCapture(roll);
    }
}
