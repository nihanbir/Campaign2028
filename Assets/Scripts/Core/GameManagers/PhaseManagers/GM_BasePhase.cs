
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

    protected bool isActive = false;
    
    private void HandleTurnEvent(TurnEvent e)
    {
        if (!isActive) return;

        switch (e.stage)
        {
            case TurnStage.PlayerRolled:
                var data = (PlayerRolledData)e.Payload;
                PlayerRolledDice(data.Player, data.Roll);
                break;
        }
    }
    
    private void OnPhaseChanged(GM_BasePhase newPhase)
    {
        if (PhaseType == newPhase.PhaseType)
            BeginPhase();
        else if (PhaseType != newPhase.PhaseType && isActive)
            EndPhase();
    }

    protected virtual void BeginPhase()
    {
        isActive = true;
        TurnFlowBus.Instance.OnEvent += HandleTurnEvent;
        
    }

    protected virtual void EndPhase()
    {
        isActive = false;
        TurnFlowBus.Instance.OnEvent -= HandleTurnEvent;
        
    }

    protected virtual void StartPlayerTurn()
    {
        TurnFlowBus.Instance.Raise(new TurnEvent(TurnStage.PlayerTurnStarted, game.CurrentPlayer));
    }

    protected virtual void EndPlayerTurn()
    {
        TurnFlowBus.Instance.Raise(new TurnEvent(TurnStage.PlayerTurnEnded, game.CurrentPlayer));
    }
    protected virtual void MoveToNextPlayer() { }

    protected virtual void PlayerRolledDice(Player player, int roll)
    {
        
    }

    
}
