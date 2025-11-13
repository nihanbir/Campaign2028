using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayerDisplayCard : SelectableDisplayCard<ActorCard>
{
    [Header("Common Elements")]
    public Player owningPlayer;
    public Image diceImage;
    public CardDisplayType displayType = CardDisplayType.UnassignedPlayer;
    
    [Header("Actor Card Elements")]
    public GameObject scorePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI evScoreText;
    public TextMeshProUGUI instScoreText;
    
    void Start()
    {
        if (GameManager.Instance == null || GameManager.Instance.players.Count == 0)
        {
            Debug.Log("GameManager or players not initialized.");
            return;
        }
        
        if (diceImage) diceImage.gameObject.SetActive(false);
        if (GameManager.Instance.CurrentPhase == GamePhase.Setup)
        {
            if (scorePanel) scorePanel.SetActive(false);
        }
        
        SetClickable(true);
        SetHoldable(false);
    }
    
    /// <summary>
    /// Sets the card type as unassigned actor
    /// </summary>
    /// <param name="card"></param>
    protected override void SetCard(ActorCard card)
    {
        cardData = card;
        displayType = CardDisplayType.UnassignedActor;
        
        SetHoldable(true);
        UpdateUI();
        
    }
    
    public void SetOwnerPlayer(Player player)
    {
        owningPlayer = player;
        
        if (displayType == CardDisplayType.UnassignedPlayer && nameText)
        {
            SetHoldable(false);
            nameText.text = $"Player {player.playerID}";
        }
        
        UpdateUI();
    }
    
#region UI 
    private void UpdateUI()
    {
        if (cardData == null) return;
        if (displayType == CardDisplayType.UnassignedPlayer) return;
        
        nameText.text = cardData.cardName;
        artworkImage.sprite = cardData.artwork;
        UpdateScore();
        
    }
    
    public void UpdateScore()
    {
        evScoreText.text = cardData.evScore.ToString();
        instScoreText.text = cardData.instScore.ToString();
    }
    
    public void ShowDice(bool show)
    {
        if (diceImage) diceImage.gameObject.SetActive(show);
    }
    
    public void SetRolledDiceImage(Sprite roll)
    {
        if (!diceImage) return;
        
        ShowDice(true);
        diceImage.sprite = roll;
    }
    
#endregion UI

#region Click Handler

    public override void OnPointerClick(PointerEventData eventData)
    {
        switch (displayType)
        {
            case CardDisplayType.UnassignedActor:
                base.OnPointerClick(eventData);
                break;
            case CardDisplayType.UnassignedPlayer:
                if (isSelected)
                    TurnFlowBus.Instance.Raise(
                        // new CardInputEvent(CardInputStage.Clicked, new PlayerClickedData(owningPlayer))
                        new CardInputEvent(CardInputStage.Clicked, owningPlayer)
                    );
                break;
            case CardDisplayType.AssignedActor:
                // Already assigned, no action
                break;
        }
    }
    
#endregion Click Handler


#region Helper Methods

    public void ConvertToAssignedActor(ActorCard actor)
    {
        cardData = actor;
        displayType = CardDisplayType.AssignedActor;
        
        SetHoldable(false);
        UpdateUI();
    }
    
    public Transform GetDiceTransform()
    {
        return diceImage != null ? diceImage.transform : null;
    }
    
#endregion Helper Methods
   

}

public enum CardDisplayType
{
    UnassignedPlayer,
    UnassignedActor,
    AssignedActor,
}

// public sealed class PlayerClickedData
// {
//     public Player player;
//     public PlayerClickedData(Player p) {player = p; }
// }