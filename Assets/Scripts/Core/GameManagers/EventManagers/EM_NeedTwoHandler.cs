
public class EM_NeedTwoHandler : BaseEventHandler
{
    private bool _needTwoActive = false;

    public EM_NeedTwoHandler(GM_MainPhase phase, EventManager parent) : base(phase, parent) { }

    public override void Handle(Player player, EventCard card, EventType effectiveType)
    {
        _needTwoActive = true;
        
        
        //TODO:Evaluate the roll here instead
        Complete(player, card);
    }

    public bool ConsumeNeedTwo()
    {
        if (!_needTwoActive) return false;
        _needTwoActive = false;
        return true;
    }
}
