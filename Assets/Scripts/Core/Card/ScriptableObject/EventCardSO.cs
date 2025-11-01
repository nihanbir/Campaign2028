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
    
    [ReadOnly] public ActorTeam benefitingTeam;
    
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
            benefitingTeam = benefitingTeam,
            
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

        if (eventType != EventType.ExtraRoll && eventType != EventType.Challenge)
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
        
        // --- Compute beneficial team ---
        if (eventType == EventType.TeamConditional)
        {
            bool blueBenefit = IsBeneficial(blueTeam);
            bool redBenefit = IsBeneficial(redTeam);

            if (blueBenefit && !redBenefit) benefitingTeam = ActorTeam.Blue;
            else if (redBenefit && !blueBenefit) benefitingTeam = ActorTeam.Red;
            else benefitingTeam = ActorTeam.None;
        }
        else
        {
            benefitingTeam = ActorTeam.None;
        }
        
        UnityEditor.EditorUtility.SetDirty(this);

    }
    
    private bool IsBeneficial(EventType effect)
    {
        switch (effect)
        {
            case EventType.ExtraRoll:
                return true;
            default:
                return false;
        }
    }
    
#endif

}