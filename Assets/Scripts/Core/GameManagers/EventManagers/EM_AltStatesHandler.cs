
using UnityEngine;

public class EM_AltStatesHandler : BaseEventHandler
{
    public EM_AltStatesHandler(GM_MainPhase phase, EventManager parent) : base(phase, parent) { }

    // Alt state vars
    private StateCard _altState1;
    private StateCard _altState2;
    public override void Handle(Player player, EventCard card, EventType effectiveType)
    {
        // NOTE: your original code looked up altState2 using altState1 by mistake.
        // Fixed to use card.altState2 for second find.
        _altState1 = _phase.FindStateFromDeck(card.altState1, out var found1);
        _altState2 = _phase.FindStateFromDeck(card.altState2, out var found2);

        // If neither found, cancel
        if (!found1 && !found2)
        {
            Cancel(card);
            return;
        }
        
        // Bus event for decoupled UI
        GameEventBus.Instance.Raise(new GameEvent(
            EventStage.AltStatesShown,
            new AltStatesData(player, _altState1, _altState2, card)));
    }
    
    public void EvaluateStateDiscard(Player player, int roll)
    {
        StateCard cardToDiscard = null;
        switch (roll)
        {
            case 1: if (_altState1 != null) cardToDiscard = _altState1; break;
            case 2: if (_altState2 != null) cardToDiscard = _altState2; break;
        }

        if (cardToDiscard != null)
        {
            _phase.DiscardState(cardToDiscard);
        }
        else
        {
            Debug.Log($"Player {player.playerID} didn't discard any states!");
        }
        
        GameManager.Instance.StartCoroutine(_parent.EndTurnAfterDelay(2f));
    }
}
