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

    protected void Complete(Player player, EventCard card)
    {
        _parent.EndEventImmediate(card, player);
    }

    protected void Cancel(EventCard card)
    {
        _parent.CancelEvent(card);
    }
}