using System.Collections.Generic;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UnassignedPlayerDisplayCard : DisplayCard, IPointerClickHandler
{
    public Player player;
    public TextMeshProUGUI playerID;
    public Image diceImage;
    public int diceRoll;

    void Start()
    {
        diceImage.gameObject.SetActive(false);
    }
    
    public void SetUnassignedPlayerCard(Player player)
    {
        this.player = player;
        playerID.text = "Player " + player.playerID;
    }

    public void SetRolledDice(int diceRoll)
    {
        diceImage.gameObject.SetActive(true);
        this.diceRoll = diceRoll;
        GameUIManager.Instance.SetDiceSprite(diceImage);
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if(!player) return;
        if (player == GameManager.Instance.CurrentPlayer) return;
        SetupPhaseUIManager.Instance.AssignSelectedActorToPlayer(player, this);
    }

}
