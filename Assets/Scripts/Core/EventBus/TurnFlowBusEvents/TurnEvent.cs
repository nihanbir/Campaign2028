public class TurnEvent : GameEventBase
{
    public readonly TurnStage stage;

    public TurnEvent(TurnStage stage, object payload = null)
        : base(payload)
    {
        this.stage = stage;
    }

    public override string GetName() => $"TurnEvent({stage})";
}

public enum TurnStage
{
    None,
    PlayerTurnStarted,
    PlayerTurnEnded,
    RollDiceRequest,
    PlayerRolled,
}

public sealed class PlayerRolledData
{
    public Player Player;
    public int    Roll;
    public PlayerRolledData(Player p, int r) { Player = p; Roll = r; }
}

public sealed class PlayerTurnStartedData
{
    public Player Player;
    public PlayerTurnStartedData(Player p) { Player = p;}
}

public sealed class PlayerTurnEndedData
{
    public Player Player;
    public PlayerTurnEndedData(Player p) { Player = p;}
}