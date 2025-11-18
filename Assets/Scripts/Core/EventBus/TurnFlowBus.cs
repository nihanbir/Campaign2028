using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TurnFlowBus
{
    private static TurnFlowBus _instance;
    public static TurnFlowBus Instance => _instance ??= new TurnFlowBus();

    public event Action<IGameEvent> OnEvent;
    public event Action<IGameEvent> OnOneTimeEvent;
    
    public void Raise(IGameEvent e)
    {
#if UNITY_EDITOR
        Debug.Log($"[TurnFlowBus] {e}");
#endif
        OnEvent?.Invoke(e);
    }

    public void Clear() => OnEvent = null;
    
    private HashSet<int> _fired = new();

    public void RaiseOnce(IGameEvent e)
    {
        if (!_fired.Add(e.EventId)) return;
#if UNITY_EDITOR
        Debug.Log($"[TurnFlowBus: One Time Event] {e}");
#endif
        OnOneTimeEvent?.Invoke(e);
    }
}