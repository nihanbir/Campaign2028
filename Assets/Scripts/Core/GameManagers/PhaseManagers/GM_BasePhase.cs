
using System;

public abstract class GM_BasePhase
{
    protected readonly GameManager game;
    protected readonly AIManager aiManager;
    public abstract GamePhase PhaseType { get; }
    
    protected GM_BasePhase()
    {
        game = GameManager.Instance;
        aiManager = AIManager.Instance;
        game.OnPhaseChanged += OnPhaseChanged;
    }
    
    private void OnPhaseChanged(GM_BasePhase newPhase)
    {
        if (newPhase == this)
            BeginPhase();
        else
            EndPhase();
    }
    
    protected virtual void BeginPhase() { }
    protected virtual void EndPhase() { }
    public virtual void StartPlayerTurn() { }
    public virtual void EndPlayerTurn() { }
    public virtual void MoveToNextPlayer() { }
    public virtual void PlayerRolledDice() { }

    
}
