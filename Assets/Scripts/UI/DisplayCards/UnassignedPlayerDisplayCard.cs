using UnityEngine.EventSystems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnassignedPlayerDisplayCard : DisplayCard, IPointerClickHandler
{
    public TextMeshProUGUI playerID;

    void Start()
    {
        diceImage.gameObject.SetActive(false);
    }
    
    public void SetUnassignedPlayerCard(Player player)
    {
        owningPlayer = player;
        playerID.text = "Player " + player.playerID;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if(!owningPlayer) return;
        if (owningPlayer == SetupPhaseGameManager.Instance.CurrentPlayer) return;
        SetupPhaseUIManager.Instance.AssignSelectedActorToPlayer(owningPlayer, this);
    }

}
