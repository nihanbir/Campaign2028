
using System;

public abstract class GM_BasePhase
{
    public abstract GamePhase PhaseType { get; }
    
    protected readonly GameManager game;
    protected readonly GameUIManager uiManager;
    protected readonly AIManager aiManager;
    
    public event Action<Player> OnPlayerTurnStarted;
    public event Action<Player> OnPlayerTurnEnded;
    
    protected GM_BasePhase()
    {
        game = GameManager.Instance;
        uiManager = GameUIManager.Instance;
        aiManager = AIManager.Instance;
        game.OnPhaseChanged += OnPhaseChanged;
    }
    
    private void OnPhaseChanged(GM_BasePhase newPhase)
    {
        if (newPhase.PhaseType == PhaseType)
            BeginPhase();
        else
            EndPhase();
    }

    protected virtual void BeginPhase()
    {
        uiManager.OnRolledDice += PlayerRolledDice;
    }

    protected virtual void EndPhase()
    {
        uiManager.OnRolledDice -= PlayerRolledDice;
    }

    public virtual void StartPlayerTurn()
    {
        OnPlayerTurnStarted?.Invoke(game.CurrentPlayer);
        
    }

    public virtual void EndPlayerTurn()
    {
        OnPlayerTurnEnded?.Invoke(game.CurrentPlayer);
        
    }
    public virtual void MoveToNextPlayer() { }

    public virtual void PlayerRolledDice(int roll)
    {
        
    }

    
}
