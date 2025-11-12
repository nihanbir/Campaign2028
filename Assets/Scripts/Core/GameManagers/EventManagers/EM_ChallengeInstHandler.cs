
using UnityEngine;

public class EM_ChallengeInstHandler : EM_ChallengeHandler
{
    public EM_ChallengeInstHandler(GM_MainPhase phase, EventManager parent) : base(phase, parent) { }
    
    public override void Handle(Player player, EventCard card, EventType effectiveType)
    {
        chosenCard = _phase.FindHeldInstitution(card.requiredInstitution, out var cardFound);
        
        if (!cardFound)
        {
            Cancel(card);
            return;
        }

        var cardHolder = _phase.GetCardHolder(chosenCard);
        if (cardHolder && cardHolder != player)
        {
            defender = cardHolder;
        }
        
        if (!defender)
        {
            Cancel(card);
            return;
        }

        // Legacy + bus
        GameEventBus.Instance.Raise(new GameEvent(EventStage.DuelStarted, new DuelData(attacker, defender, chosenCard, card)));
    }
}
