
using UnityEngine;

public class EM_AltStatesHandler : BaseEventHandler
{
    public EM_AltStatesHandler(GM_MainPhase phase, EventManager parent) : base(phase, parent) { }

    // Alt state vars
    private StateCard _altState1;
    private StateCard _altState2;
    public override void Handle(Player player, EventCard card, EventType effectiveType)
    {
        _altState1 = phase.FindStateFromDeck(card.altState1, out var found1);
        _altState2 = phase.FindStateFromDeck(card.altState2, out var found2);

        // If neither found, cancel
        if (!found1 && !found2)
        {
            Cancel(card);
            return;
        }

        parent.IsEventScreen = true;
        TurnFlowBus.Instance.Raise(new EventCardEvent(EventStage.ChangeToEventScreen));
        
        // Bus event for decoupled UI
        EventCardBus.Instance.Raise(new EventCardEvent(
            EventStage.AltStatesShown,
            new AltStatesData(player, _altState1, _altState2, card)));
    }
    
    public override void EvaluateRoll(Player player, int roll)
    {
        StateCard cardToDiscard = null;
        switch (roll)
        {
            case 1: if (_altState1 != null) cardToDiscard = _altState1; break;
            case 2: if (_altState2 != null) cardToDiscard = _altState2; break;
        }

        if (cardToDiscard != null)
        {
            phase.DiscardState(cardToDiscard);
        }
        else
        {
            Debug.Log($"Player {player.playerID} didn't discard any states!");
        }
        
        parent.CompleteDuel();
    }
}
