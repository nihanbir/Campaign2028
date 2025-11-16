
public interface IEventHandler
{
    void Handle(Player player, EventCard card, EventType effectiveType);
    void EvaluateRoll(Player player, int roll) {}
    
}
