using UnityEngine.EventSystems;
using TMPro;

public class UnassignedPlayerDisplayCard : DisplayCard, IPointerClickHandler
{
    public Player player;
    public TextMeshProUGUI playerID;

    public void SetUnassignedPlayerCard(Player player)
    {
        this.player = player;
        playerID.text = "Player " + player.playerID;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if(!player) return;
        SetupPhaseUIManager.Instance.AssignSelectedActorToPlayer(player, this);
    }
}
