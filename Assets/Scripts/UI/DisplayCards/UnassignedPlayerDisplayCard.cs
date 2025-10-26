using UnityEngine.EventSystems;

public class UnassignedPlayerDisplayCard : DisplayCard, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        SetupPhaseUIManager.Instance.AssignSelectedActorToPlayer(owningPlayer, this);
    }

    public override void SetOwnerPlayer(Player player)
    {
        base.SetOwnerPlayer(player);
        playerID.text = "Player " + player.playerID;
    }
}
