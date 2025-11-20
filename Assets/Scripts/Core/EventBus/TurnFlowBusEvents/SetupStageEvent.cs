using System.Collections.Generic;

public class SetupStageEvent : GameEventBase
{
    public readonly SetupStage stage;

    public SetupStageEvent(SetupStage stage, object payload = null)
        : base(payload)
    {
        this.stage = stage;
    }

    public override int EventId => (int)stage;
    public override string GetName() => $"SetupStageEvent({stage})";
}

public sealed class UniqueWinnerData
{
    public Player player;
    public UniqueWinnerData(Player p) { player = p;}
}

public sealed class TiedRollData
{
    public List<Player> players;
    public TiedRollData(List<Player> p) { players = p;}
}

public sealed class ActorAssignedData
{
    public Player player;
    public ActorCard actor;
    public ActorAssignedData(Player p, ActorCard a) { player = p; actor = a; }
}

public sealed class AllegianceAssignedData
{
    public Player player;
    public AllegianceCard allegiance;
    public AllegianceAssignedData(Player p, AllegianceCard a) { player = p; allegiance = a; }
}

public sealed class BeginPhaseData
{
    public List<Player> unassignedPlayers;
    public List<ActorCard> unassignedActors;
    
    public BeginPhaseData(List<Player> p, List<ActorCard> a) { unassignedPlayers = p; unassignedActors = a; }
}

public enum SetupStage
{
    None,
    BeginPhase,
    Roll,
    AllPlayersRolled,
    UniqueWinner,
    TiedRoll,
    Reroll,
    BeginActorAssignment,
    ActorAssigned,
    AllegianceAssigned,
    LastActorAssigned
}