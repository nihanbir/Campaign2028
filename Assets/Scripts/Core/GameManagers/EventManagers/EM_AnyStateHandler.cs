
using System.Collections.Generic;
using UnityEngine;

public class EM_AnyStateHandler : EM_ChallengeHandler
{
    public EM_AnyStateHandler(GM_MainPhase phase, EventManager parent) : base(phase, parent) { }
    
    public override void Handle(Player player, EventCard card, EventType effectiveType)
    {
        var availableStates = GetChallengableStatesForPlayer(player);
        if (availableStates == null)
        {
            Cancel(card);
            return;
        }

        // Legacy + bus
        
        EventCardBus.Instance.Raise(new EventCardEvent(EventStage.ChallengeStateShown, new ChallengeStatesData(player, availableStates, card)));
    }
    
    private List<StateCard> GetChallengableStatesForPlayer(Player player)
    {
        var stateOwners     = _phase.GetStateOwners();
        var availableStates = new List<StateCard>();

        foreach (var kvp in stateOwners)
        {
            if (kvp.Value == player) continue;
            availableStates.Add(kvp.Key);
        }

        return availableStates.Count == 0 ? null : availableStates;
    }
    
    public void HandleStateChosen(Player player, StateCard chosenState)
    {
        defender = _phase.GetCardHolder(chosenState);
        chosenCard = chosenState;

        // Defender could be null if something desynced; cancel safely
        if (!defender)
        {
            Cancel(currentEventCard);
            return;
        }
        
        EventCardBus.Instance.Raise(new EventCardEvent(EventStage.DuelStarted, new DuelData(player, defender, chosenState, currentEventCard)));
    }
}
