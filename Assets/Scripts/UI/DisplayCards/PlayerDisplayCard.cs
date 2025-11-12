using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayerDisplayCard : SelectableDisplayCard<ActorCard>
{
    [Header("Common Elements")]
    public Player owningPlayer;
    public Image diceImage;
    public CardDisplayType displayType;
    
    [Header("Actor Card Elements")]
    public GameObject scorePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI evScoreText;
    public TextMeshProUGUI instScoreText;

    public static event Action<PlayerDisplayCard> OnPlayerCardClicked; 
    
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
        
    }

    protected override void SetCard(ActorCard card)
    {
        cardData = card;
        displayType = CardDisplayType.UnassignedActor;
        UpdateUI();
        
    }
    public void SetOwnerPlayer(Player player)
    {
        owningPlayer = player;
        
        if (displayType == CardDisplayType.UnassignedPlayer && nameText)
        {
            nameText.text = $"Player {player.playerID}";
        }
        
        UpdateUI();
    }
    
#region UI 
    private void UpdateUI()
    {
        if (cardData != null && displayType != CardDisplayType.UnassignedPlayer)
        {
            UpdateActorUI();
        }
    }
    private void UpdateActorUI()
    {
        if (cardData == null) return;
        
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
        if (diceImage)
        {
            ShowDice(true);
            diceImage.sprite = roll;
        }
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
                OnPlayerCardClicked?.Invoke(this);
                break;
            case CardDisplayType.AssignedActor:
                // Already assigned, no action
                break;
        }
    }
    
#endregion Click Handler
public Transform GetDiceTransform()
{
    return diceImage != null ? diceImage.transform : null;
}

#region Helper Methods

    public void ConvertToAssignedActor(ActorCard actor)
    {
        cardData = actor;
        displayType = CardDisplayType.AssignedActor;
        UpdateUI();
    }
    
#endregion Helper Methods
   

}

public enum CardDisplayType
{
    UnassignedPlayer,
    UnassignedActor,
    AssignedActor,
}