using UnityEngine.EventSystems;

public class UnassignedPlayerDisplayCard : DisplayCard, IPointerClickHandler
{
    
    void Start()
    {
        diceImage.gameObject.SetActive(false);
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        SetupPhaseUIManager.Instance.AssignSelectedActorToPlayer(owningPlayer, this);
    }

}
