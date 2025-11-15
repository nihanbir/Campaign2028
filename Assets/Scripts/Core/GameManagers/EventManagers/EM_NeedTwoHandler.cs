
public class EM_NeedTwoHandler : BaseEventHandler
{
    public EM_NeedTwoHandler(GM_MainPhase phase, EventManager parent) : base(phase, parent) { }

    public override void Handle(Player player, EventCard card, EventType effectiveType)
    {
        
        //TODO:Evaluate the roll here instead
        Complete(player, card);
    }
    
}
