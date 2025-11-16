
public class EM_ChallengeInstHandler : EM_ChallengeHandler
{
    private readonly EM_ChallengeHandler _parentHandler;
    public EM_ChallengeInstHandler(GM_MainPhase phase, EventManager parent, EM_ChallengeHandler parentHandler) :
        base(phase, parent)
    {
        _parentHandler = parentHandler;
    }
    
    public override void Handle(Player player, EventCard card, EventType effectiveType)
    {
        var chosenCard = _phase.FindHeldInstitution(card.requiredInstitution, out var cardFound);
        
        if (!cardFound)
        {
            Cancel(card);
            return;
        }

        Player defender = null;
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
        TurnFlowBus.Instance.Raise(new EventCardEvent(EventStage.ChangeToEventScreen));
        
        _parentHandler.SetChosenCard(chosenCard);
        EventCardBus.Instance.Raise(new EventCardEvent(EventStage.DuelStarted, new DuelData(player, defender, chosenCard, card)));
    }
}
