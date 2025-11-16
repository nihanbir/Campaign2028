
public class EM_LoseTurnHandler : BaseEventHandler
{
    public EM_LoseTurnHandler(GM_MainPhase phase, EventManager parent) : base(phase, parent) { }

    public override void Handle(Player player, EventCard card, EventType effectiveType)
    {
        // Broadcast start so UI can show small feedback if desired
        EventCardBus.Instance.Raise(new EventCardEvent(EventStage.EventStarted, new EventStartedData(effectiveType, player, card)));

        //TODO: maybe instead of this end player turn without moving to next player
        
        // Small delay to let any UI coroutines/animations breathe; then end turn
        _parent.CompleteEvent();
    }
}
