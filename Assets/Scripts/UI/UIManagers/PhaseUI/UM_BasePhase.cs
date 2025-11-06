
using System;
using UnityEngine;

public abstract class UM_BasePhase : MonoBehaviour
{
    protected GameManager game;
    protected GM_BasePhase phase;
    public abstract GamePhase PhaseType { get; }
    
    
    private void Awake()
    {
        game = GameManager.Instance;
        game.OnPhaseChanged += OnPhaseChanged;
        
    }
    private void OnDestroy()
    {
        if (game != null)
            game.OnPhaseChanged -= OnPhaseChanged;
    }
    
    private void OnPhaseChanged(GM_BasePhase newPhase)
    {
        phase = newPhase;
        if (newPhase.PhaseType == PhaseType)
            OnPhaseEnabled();
        else
            OnPhaseDisabled();
    }
    
    public virtual void OnPhaseEnabled()  { gameObject.SetActive(true);  }
    public virtual void OnPhaseDisabled() { gameObject.SetActive(false); }
    
}
