
public class EM_LoseTurnHandler : BaseEventHandler
{
    public EM_LoseTurnHandler(GM_MainPhase phase, EventManager parent) : base(phase, parent) { }

    public override void Handle(Player player, EventCard card, EventType effectiveType)
    {
        EventCardBus.Instance.Raise(new EventCardEvent(EventStage.LoseTurn));
        
        parent.CompleteEvent();
        
        phase.EndPlayerTurnFromEvent();
    }
}
