
using UnityEngine;

public class EM_NeedTwoHandler : BaseEventHandler
{
    public EM_NeedTwoHandler(GM_MainPhase phase, EventManager parent) : base(phase, parent) { }

    public override void Handle(Player player, EventCard card, EventType effectiveType)
    {
        
    }
    
    // public void EvaluateCapture(int roll)
    // {
    //     bool success = chosenCard switch
    //     {
    //         StateCard s => s.IsSuccessfulRoll(roll, attacker.assignedActor.team),
    //         InstitutionCard i => i.IsSuccessfulRoll(roll, attacker.assignedActor.team),
    //         _                 => false
    //     };
    //
    //     Debug.Log($"rolled: {roll}");
    //        
    //     if (success)
    //     {
    //         _phase.UpdateCardOwnership(attacker, chosenCard);
    //     }
    //     else
    //     {
    //         _phase.ReturnCardToDeck(currentEventCard);
    //         Debug.Log($"Player {attacker.playerID} failed to capture {chosenCard.cardName}");
    //     }
    //     
    //     //TODO: duel completed instead
    //     _parent.CompleteEvent();
    // }
    
}
