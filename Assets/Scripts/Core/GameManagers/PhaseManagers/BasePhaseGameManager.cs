
public abstract class BasePhaseGameManager
{
    protected readonly GameManager game;

    protected BasePhaseGameManager(GameManager gm)
    {
        game = gm;
    }

    public virtual void InitializePhase() { }
    public virtual void StartPlayerTurn() { }
    
    public virtual void EndPlayerTurn() { }
    public virtual void MoveToNextPlayer() { }
    
    public virtual void PlayerRolledDice() { }
    
    
    
}
