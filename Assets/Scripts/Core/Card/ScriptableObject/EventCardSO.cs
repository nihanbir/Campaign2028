using UnityEngine;

[CreateAssetMenu(fileName = "New Event", menuName = "Campaign 2028/Event Card")]
public class EventCardSO : ScriptableObject
{
    public string eventName;
    public Sprite artwork;
    
    public EventType eventType;
    public EventSubType subType;
    
    [SerializeField]
    public InstitutionCardSO requiredInstitution;
    
    public bool mustPlayImmediately;
    public bool canSave;
    
    public EventCard ToCard()
    {
        return new EventCard
        {
            cardName = eventName,
            artwork = artwork,
            eventType = eventType,
            subType = subType,
            mustPlayImmediately = mustPlayImmediately,
            canSave = canSave,
            requiredInstitution = requiredInstitution != null ? requiredInstitution.ToCard() : null
        };
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (subType == EventSubType.ExtraRoll_IfHasInstitution && requiredInstitution == null)
        {
            Debug.LogWarning($"Event '{eventName}' requires an institution but none is assigned.", this);
        }
    }
#endif
}