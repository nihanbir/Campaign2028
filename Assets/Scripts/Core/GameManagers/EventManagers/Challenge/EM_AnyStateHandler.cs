
using System.Collections.Generic;

public class EM_AnyStateHandler : EM_ChallengeHandler
{
    private readonly EM_ChallengeHandler _parentHandler;
    private Player _currentPlayer;

    public EM_AnyStateHandler(GM_MainPhase phase, EventManager parent, EM_ChallengeHandler parentHandler) : base(phase,
        parent)
    {
        _parentHandler = parentHandler;
    }
    
    public override void Handle(Player player, EventCard card, EventType effectiveType)
    {
        var availableStates = GetChallengableStatesForPlayer(player);
        if (availableStates == null)
        {
            Cancel(card);
            return;
        }

        SelectableCardBus.Instance.OnEvent += OnCardEvent;

        _currentPlayer = player;
        
        parent.IsEventScreen = true;
        TurnFlowBus.Instance.Raise(new EventCardEvent(EventStage.ChangeToEventScreen));

        EventCardBus.Instance.Raise(new EventCardEvent(EventStage.ChallengeStatesDetermined, new ChallengeStatesData(player, availableStates, card)));
    }
    
    private void OnCardEvent(CardInputEvent e)
    {
        switch (e.stage)
        {
            case CardInputStage.Held:
                HandleStateCardHeldRequest(_currentPlayer, (StateCard)e.payload);
                break;
        }
    }
    
    private List<StateCard> GetChallengableStatesForPlayer(Player player)
    {
        var stateOwners     = phase.GetStateOwners();
        var availableStates = new List<StateCard>();

        foreach (var kvp in stateOwners)
        {
            if (kvp.Value == player) continue;
            availableStates.Add(kvp.Key);
        }

        return availableStates.Count == 0 ? null : availableStates;
    }
    
    private void HandleStateCardHeldRequest(Player player, StateCard chosenState)
    {
        SelectableCardBus.Instance.OnEvent -= OnCardEvent;
        
        var defender = phase.GetCardHolder(chosenState);

        // Defender could be null if something desynced; cancel safely
        if (defender == null)
        {
            Cancel(currentEventCard);
            return;
        }
        
        _parentHandler.SetChosenCard(chosenState);
        EventCardBus.Instance.Raise(new EventCardEvent(EventStage.DuelStarted, new DuelData(player, defender, chosenState, currentEventCard)));
    }
}
