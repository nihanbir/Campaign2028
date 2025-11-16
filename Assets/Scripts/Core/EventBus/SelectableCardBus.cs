using System;
using UnityEngine;

public sealed class SelectableCardBus
{
    private static SelectableCardBus _instance;
    public static SelectableCardBus Instance => _instance ??= new SelectableCardBus();

    public event Action<CardInputEvent> OnEvent;

    public void Raise(CardInputEvent e)
    {
#if UNITY_EDITOR
        Debug.Log($"[EventBus] {e.stage}");
#endif
        OnEvent?.Invoke(e);
    }

    public void Clear() => OnEvent = null;
}