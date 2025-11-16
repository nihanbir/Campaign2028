using UnityEngine;

public abstract class BaseEventHandler : IEventHandler
{
    protected readonly GM_MainPhase _phase;
    protected readonly EventManager _parent;

    protected BaseEventHandler(GM_MainPhase phase, EventManager parent)
    {
        _phase = phase;
        _parent = parent;
    }

    public abstract void Handle(Player player, EventCard card, EventType effectiveType);
    
    public virtual void EvaluateRoll(Player player, int roll) {}

    protected void Cancel(EventCard card)
    {
        _parent.CancelEvent(card);
    }
}