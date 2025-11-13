using System.Collections.Generic;

public class SetupStageEvent : GameEventBase
{
    public readonly SetupStage stage;

    public SetupStageEvent(SetupStage stage, object payload = null)
        : base(payload)
    {
        this.stage = stage;
    }

    public override string GetName() => $"SetupStageEvent({stage})";
}

public sealed class UniqueWinner
{
    public Player player;
    public UniqueWinner(Player p) { player = p;}
}

public sealed class TiedRoll
{
    public List<Player> players;
    public TiedRoll(List<Player> p) { players = p;}
}

public sealed class ActorAssigned
{
    public Player player;
    public ActorCard actor;
    public ActorAssigned(Player p, ActorCard a) { player = p; actor = a; }
}

public enum SetupStage
{
    None,
    Roll,
    AllPlayersRolled,
    UniqueWinner,
    TiedRoll,
    Reroll,
    BeginActorAssignment,
    ActorAssigned,
    LastActorAssigned
}