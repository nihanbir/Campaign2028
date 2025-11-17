using UnityEngine;

public abstract class BaseEventHandler : IEventHandler
{
    protected readonly GM_MainPhase phase;
    protected readonly EventManager parent;

    protected BaseEventHandler(GM_MainPhase phase, EventManager parent)
    {
        this.phase = phase;
        this.parent = parent;
    }

    public abstract void Handle(Player player, EventCard card, EventType effectiveType);
    
    public virtual void EvaluateRoll(Player player, int roll) {}

    protected void Cancel(EventCard card)
    {
        parent.CancelEvent(card);
    }
}