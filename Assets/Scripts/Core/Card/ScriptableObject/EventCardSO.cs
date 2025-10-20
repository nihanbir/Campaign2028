using UnityEngine;

[CreateAssetMenu(fileName = "New Event", menuName = "Campaign 2028/Event Card")]
public class EventCardSO : ScriptableObject
{
    public string eventName;
    public Sprite artwork;
    public Sprite backSide;
    
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