
using UnityEngine;
using UnityEngine.EventSystems;

public class BaseDisplayCard : MonoBehaviour, IPointerClickHandler
{
    [Header("Card Type")]
    public CardDisplayType displayType;

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        
    }
}

public enum CardDisplayType
{
    UnassignedPlayer,
    UnassignedActor,
    AssignedActor,
    Event,
}
