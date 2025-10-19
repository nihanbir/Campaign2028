using UnityEngine;

[CreateAssetMenu(fileName = "New Event", menuName = "Campaign 2028/Event Card")]
public class EventCardSO : ScriptableObject
{
    public string eventName;
    [TextArea(2, 4)]
    public string description;
    public Sprite artwork;
    
    public EventType eventType;
    public bool mustPlayImmediately;
    public bool canSave;
    
    public EventCard ToCard()
    {
        return new EventCard
        {
            cardName = eventName,
            artwork = artwork,
            eventType = eventType,
            mustPlayImmediately = mustPlayImmediately,
            canSave = canSave
        };
    }
}