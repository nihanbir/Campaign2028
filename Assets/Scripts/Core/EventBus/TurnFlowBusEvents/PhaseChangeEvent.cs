public class PhaseChangeEvent : GameEventBase
{
    public readonly GamePhase stage;

    public PhaseChangeEvent(GamePhase stage, object payload = null)
        : base(payload)
    {
        this.stage = stage;
    }

    public override int EventId => (int)stage;
    public override string GetName() => $"PhaseChangeEvent({stage})";
}

public sealed class GameOverData
{
    public Player Winner;
    public VictoryType VictoryType;
    
    public GameOverData(Player winner, VictoryType victoryType)
    {
        Winner = winner;
        VictoryType = victoryType;
    }
}

public enum VictoryType
{
    ElectoralVotes,
    Institutions
}