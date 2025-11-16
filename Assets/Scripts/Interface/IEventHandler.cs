
public interface IEventHandler
{
    void Handle(Player player, EventCard card, EventType effectiveType);
    public virtual void EvaluateRoll(Player player, int roll) {}
    
}
