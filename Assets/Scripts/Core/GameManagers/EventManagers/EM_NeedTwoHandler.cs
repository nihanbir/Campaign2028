
using UnityEngine;

public class EM_NeedTwoHandler : BaseEventHandler
{
    public EM_NeedTwoHandler(GM_MainPhase phase, EventManager parent) : base(phase, parent) { }

    public override void Handle(Player player, EventCard card, EventType effectiveType) { }
    
    public override void EvaluateRoll(Player player, int roll)
    {
        if (phase.CurrentTargetCard is StateCard state)
        {
            if (state.hasSecession && roll == 1)
            {
                Debug.Log($"Player {player.playerID} rolled: {roll} and {state.cardName} had secession");
                
                phase.DiscardState(state);
                parent.CompleteEvent();
                return;
            }

            if (state.hasRollAgain && roll == 4)
                player.AddExtraRoll();
        }

        if (roll == 2)
            phase.CaptureCard(player, phase.CurrentTargetCard);
        
        else
            Debug.Log($"Player {player.playerID} failed to capture {phase.CurrentTargetCard.cardName}");
        
        
        parent.CompleteEvent();
    }
    
}
