
using UnityEngine;

public class EventManager
{
    protected readonly MainPhaseGameManager game;
    
    public EventManager(MainPhaseGameManager gm)
    {
        game = gm;
    }
    
    public void ApplyEvent(EventCard card)
    {
        switch (card.eventType)
        {
            case EventType.ExtraRoll:
                HandleExtraRoll(card.subType, card);
                break;
            
            case EventType.NeedTwo:
                // HandleNeedTwo(player, card.subType);
                break;
                
            // Add other types as needed
        }
    }

    private void HandleExtraRoll(EventSubType subType, EventCard card)
    {
        var player = GameManager.Instance.CurrentPlayer;
        bool canApply = subType switch
        {
            EventSubType.ExtraRoll_IfHasInstitution => player.HasInstitution(card.requiredInstitution.ToCard()),
            EventSubType.ExtraRoll_Any => true,
            _ => false
        };

        if (canApply)
        {
            Debug.Log($"{player.playerID} gets an extra roll!");
            player.AddExtraRoll();
        }
        else
        {
            Debug.Log($"{player.playerID} cannot use this extra roll event.");
        }
    }
}
