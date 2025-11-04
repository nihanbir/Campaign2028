using UnityEngine;

[CreateAssetMenu(fileName = "New Event", menuName = "Campaign 2028/Event Card")]
public class EventCardSO : ScriptableObject
{
    public string eventName;
    public Sprite artwork;
    
    public EventType eventType;
    
    [SerializeField]
    public EventConditions eventConditions;
    public InstitutionCardSO requiredInstitution;
    public EventType blueTeam;
    public EventType redTeam;
    
    [ReadOnly] public ActorTeam benefitingTeam;
    
    public bool mustPlayImmediately;
    public bool canSave;
    public bool canReturnToDeck;
    private bool _requiresInstitution = false;
    
    public EventCard ToCard()
    {
        return new EventCard
        {
            cardName = eventName,
            artwork = artwork,
            eventType = eventType,
            mustPlayImmediately = mustPlayImmediately,
            canSave = canSave,
            canReturnToDeck = canReturnToDeck,
            requiredInstitution = requiredInstitution != null ? requiredInstitution.ToCard() : null,
            blueTeam = eventType == EventType.TeamBased ? blueTeam : EventType.None,
            redTeam = eventType == EventType.TeamBased ? redTeam : EventType.None,
            benefitingTeam = benefitingTeam,
            eventConditions = eventConditions,
            
        };
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        // --- Reset irrelevant data based on event type ---
        if (eventType != EventType.TeamBased)
        {
            blueTeam = EventType.None;
            redTeam = EventType.None;
        }

        _requiresInstitution = eventConditions is EventConditions.IfInstitutionCaptured or EventConditions.IfOwnsInstitution;

        switch (_requiresInstitution)
        {
            case false:
                requiredInstitution = null;
                break;
            // --- Validation warnings ---
            case true when requiredInstitution == null:
                Debug.LogWarning($"[{eventName}] requires an institution but none is assigned.", this);
                break;
        }

        if (eventType == EventType.TeamBased && (blueTeam == EventType.None || redTeam == EventType.None))
        {
            Debug.LogWarning($"[{eventName}] TeamConditional event requires both Blue and Red team outcomes.", this);
        }
        
        // --- Compute beneficial team ---
        if (eventType == EventType.TeamBased)
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