
using System;

public abstract class GM_BasePhase
{
    public abstract GamePhase PhaseType { get; }
    
    protected readonly GameManager game;
    protected readonly AIManager aiManager;
    
    protected GM_BasePhase()
    {
        game = GameManager.Instance;
        aiManager = AIManager.Instance;
        game.OnPhaseChanged += OnPhaseChanged;
        
    }

    protected bool isActive = false;
    
    private void HandleTurnEvent(IGameEvent e)
    {
        if (!isActive) return;

        if (e is TurnEvent t)
        {
            switch (t.stage)
            {
                case TurnStage.PlayerRolled:
                    var data = (PlayerRolledData)t.Payload;
                    PlayerRolledDice(data.Player, data.Roll);
                    break;
            }
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
        TurnFlowBus.Instance.Raise(new TurnEvent(TurnStage.PlayerTurnStarted, new PlayerTurnStartedData(game.CurrentPlayer)));
    }

    protected virtual void EndPlayerTurn()
    {
        TurnFlowBus.Instance.Raise(new TurnEvent(TurnStage.PlayerTurnEnded, new PlayerTurnEndedData(game.CurrentPlayer)));
    }
    protected virtual void MoveToNextPlayer() { }

    protected virtual void PlayerRolledDice(Player player, int roll)
    {
        
    }

    
}
