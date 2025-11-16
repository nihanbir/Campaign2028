using System;
using UnityEngine;

public sealed class EventCardBus
{
    private static EventCardBus _instance;
    public static EventCardBus Instance => _instance ??= new EventCardBus();

    public event Action<EventCardEvent> OnEvent;

    public void Raise(EventCardEvent e)
    {
#if UNITY_EDITOR
        Debug.Log($"[EventBus] {e.stage}");
#endif
        OnEvent?.Invoke(e);
    }

    public void Clear() => OnEvent = null;
}