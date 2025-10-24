using UnityEngine.EventSystems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnassignedPlayerDisplayCard : DisplayCard, IPointerClickHandler
{
    public Player owningPlayer;
    public TextMeshProUGUI playerID;
    public Image diceImage;
    public int diceRoll;

    void Start()
    {
        diceImage.gameObject.SetActive(false);
    }
    
    public void SetUnassignedPlayerCard(Player player)
    {
        owningPlayer = player;
        playerID.text = "Player " + player.playerID;
    }

    public void SetRolledDice(int diceRoll)
    {
        diceImage.gameObject.SetActive(true);
        this.diceRoll = diceRoll;
        Debug.Log(diceRoll);
        GameUIManager.Instance.SetDiceSprite(diceImage);
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if(!owningPlayer) return;
        if (owningPlayer == SetupPhaseGameManager.Instance.CurrentPlayer) return;
        SetupPhaseUIManager.Instance.AssignSelectedActorToPlayer(owningPlayer, this);
    }

}
