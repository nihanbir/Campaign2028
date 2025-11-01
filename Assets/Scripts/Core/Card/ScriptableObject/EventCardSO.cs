using UnityEngine;

[CreateAssetMenu(fileName = "New Event", menuName = "Campaign 2028/Event Card")]
public class EventCardSO : ScriptableObject
{
    public string eventName;
    public Sprite artwork;
    
    public EventType eventType;
    
    [SerializeField]
    public EventSubType subType;
    public InstitutionCardSO requiredInstitution;
    public EventType blueTeam;
    public EventType redTeam;
    
    public bool mustPlayImmediately;
    public bool canSave;
    public bool canReturnToDeck;
    
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
            canReturnToDeck = canReturnToDeck,
            requiredInstitution = requiredInstitution != null ? requiredInstitution.ToCard() : null,
            blueTeam = eventType == EventType.TeamConditional ? blueTeam : EventType.None,
            redTeam = eventType == EventType.TeamConditional ? redTeam : EventType.None,
        };
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        // --- Reset irrelevant data based on event type ---
        if (eventType != EventType.TeamConditional)
        {
            blueTeam = EventType.None;
            redTeam = EventType.None;
        }

        if (eventType != EventType.ExtraRoll)
        {
            subType = EventSubType.None;
            requiredInstitution = null;
        }

        // --- Reset irrelevant data based on subtype ---
        if (subType != EventSubType.ExtraRoll_IfHasInstitution)
        {
            requiredInstitution = null;
        }

        // --- Validation warnings ---
        if (subType == EventSubType.ExtraRoll_IfHasInstitution && requiredInstitution == null)
        {
            Debug.LogWarning($"[{eventName}] requires an institution but none is assigned.", this);
        }

        if (eventType == EventType.TeamConditional && (blueTeam == EventType.None || redTeam == EventType.None))
        {
            Debug.LogWarning($"[{eventName}] TeamConditional event requires both Blue and Red team outcomes.", this);
        }
        
        UnityEditor.EditorUtility.SetDirty(this);

    }
#endif

}