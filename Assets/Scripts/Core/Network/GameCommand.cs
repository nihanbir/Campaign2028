using UnityEngine;

/// <summary>
/// Base class for all game commands. Commands represent player intentions
/// that need validation before execution.
/// </summary>
public abstract class GameCommand
{
    public int PlayerId { get; protected set; }
    public float Timestamp { get; protected set; }
    
    protected GameCommand(int playerId)
    {
        PlayerId = playerId;
        Timestamp = Time.time;
    }
    
    /// <summary>
    /// Validate if this command can be executed in the current game state.
    /// Override in derived classes.
    /// </summary>
    public abstract bool Validate(GameStateManager state);
    
    /// <summary>
    /// Execute the command, modifying game state.
    /// Override in derived classes.
    /// </summary>
    public abstract void Execute(GameStateManager state);
    
    /// <summary>
    /// Serialize command for network transmission.
    /// Override in derived classes if needed.
    /// </summary>
    public virtual byte[] Serialize()
    {
        // Basic serialization - expand as needed
        return new byte[0];
    }
    
    /// <summary>
    /// Deserialize command from network data.
    /// </summary>
    public static GameCommand Deserialize(byte[] data)
    {
        // Implement based on your networking solution
        return null;
    }
}

// === Concrete Command Classes ===

public class RollDiceCommand : GameCommand
{
    public RollDiceCommand(int playerId) : base(playerId) { }
    
    public override bool Validate(GameStateManager state)
    {
        if (state.CurrentPlayerId != PlayerId)
            return false;
            
        var playerState = state.GetPlayerState(PlayerId);
        if (playerState == null)
            return false;
            
        return playerState.CanRoll;
    }
    
    public override void Execute(GameStateManager state)
    {
        // Server generates roll, clients receive the result
        int roll = Random.Range(1, 7);
        state.ProcessDiceRoll(PlayerId, roll);
    }
}

public class AssignActorCommand : GameCommand
{
    public int TargetPlayerId { get; private set; }
    public string ActorCardId { get; private set; }
    
    public AssignActorCommand(int playerId, int targetPlayerId, string actorCardId) 
        : base(playerId)
    {
        TargetPlayerId = targetPlayerId;
        ActorCardId = actorCardId;
    }
    
    public override bool Validate(GameStateManager state)
    {
        if (state.CurrentPlayerId != PlayerId)
            return false;
            
        if (state.CurrentPhase != GamePhase.Setup)
            return false;
            
        if (TargetPlayerId == PlayerId)
            return false;
            
        // Check if actor is still available
        if (!state.IsActorAvailable(ActorCardId))
            return false;
            
        return true;
    }
    
    public override void Execute(GameStateManager state)
    {
        state.AssignActor(TargetPlayerId, ActorCardId);
    }
}

public class DrawEventCardCommand : GameCommand
{
    public DrawEventCardCommand(int playerId) : base(playerId) { }
    
    public override bool Validate(GameStateManager state)
    {
        if (state.CurrentPlayerId != PlayerId)
            return false;
            
        if (state.CurrentPhase != GamePhase.MainGame)
            return false;
            
        return state.HasEventCardsRemaining();
    }
    
    public override void Execute(GameStateManager state)
    {
        state.DrawEventCard(PlayerId);
    }
}

public class DrawTargetCardCommand : GameCommand
{
    public DrawTargetCardCommand(int playerId) : base(playerId) { }
    
    public override bool Validate(GameStateManager state)
    {
        if (state.CurrentPlayerId != PlayerId)
            return false;
            
        if (state.CurrentPhase != GamePhase.MainGame)
            return false;
            
        return state.HasTargetCardsRemaining() && state.CurrentTargetCardId == null;
    }
    
    public override void Execute(GameStateManager state)
    {
        state.DrawTargetCard(PlayerId);
    }
}

public class PlayEventCardCommand : GameCommand
{
    public string EventCardId { get; private set; }
    
    public PlayEventCardCommand(int playerId, string eventCardId) : base(playerId)
    {
        EventCardId = eventCardId;
    }
    
    public override bool Validate(GameStateManager state)
    {
        if (state.CurrentPlayerId != PlayerId)
            return false;
            
        return state.CurrentEventCardId == EventCardId;
    }
    
    public override void Execute(GameStateManager state)
    {
        state.ApplyEventCard(PlayerId, EventCardId);
    }
}

public class SaveEventCardCommand : GameCommand
{
    public string EventCardId { get; private set; }
    
    public SaveEventCardCommand(int playerId, string eventCardId) : base(playerId)
    {
        EventCardId = eventCardId;
    }
    
    public override bool Validate(GameStateManager state)
    {
        if (state.CurrentPlayerId != PlayerId)
            return false;
            
        var playerState = state.GetPlayerState(PlayerId);
        if (playerState.HeldEventCardId != null)
            return false;
            
        var eventCard = state.GetEventCard(EventCardId);
        return eventCard?.canSave ?? false;
    }
    
    public override void Execute(GameStateManager state)
    {
        state.SaveEventCard(PlayerId, EventCardId);
    }
}

public class SelectChallengeStateCommand : GameCommand
{
    public string StateCardId { get; private set; }
    
    public SelectChallengeStateCommand(int playerId, string stateCardId) : base(playerId)
    {
        StateCardId = stateCardId;
    }
    
    public override bool Validate(GameStateManager state)
    {
        // Add validation logic
        return true;
    }
    
    public override void Execute(GameStateManager state)
    {
        state.SelectChallengeState(PlayerId, StateCardId);
    }
}

// === Setup Phase Commands ===

public class SetupRollDiceCommand : GameCommand
{
    public SetupRollDiceCommand(int playerId) : base(playerId) { }
    
    public override bool Validate(GameStateManager state)
    {
        if (state.CurrentPhase != GamePhase.Setup)
            return false;
            
        return state.CanPlayerRollInSetup(PlayerId);
    }
    
    public override void Execute(GameStateManager state)
    {
        int roll = Random.Range(1, 7);
        state.ProcessSetupRoll(PlayerId, roll);
    }
}

public class SelectActorCommand : GameCommand
{
    public string ActorCardId { get; private set; }
    
    public SelectActorCommand(int playerId, string actorCardId) : base(playerId)
    {
        ActorCardId = actorCardId;
    }
    
    public override bool Validate(GameStateManager state)
    {
        if (state.CurrentPhase != GamePhase.Setup)
            return false;
            
        if (state.SetupStage != SetupStage.BeginActorAssignment)
            return false;
            
        return state.IsActorAvailable(ActorCardId);
    }
    
    public override void Execute(GameStateManager state)
    {
        state.SelectActor(PlayerId, ActorCardId);
    }
}

public class ConfirmActorAssignmentCommand : GameCommand
{
    public int TargetPlayerId { get; private set; }
    
    public ConfirmActorAssignmentCommand(int playerId, int targetPlayerId) : base(playerId)
    {
        TargetPlayerId = targetPlayerId;
    }
    
    public override bool Validate(GameStateManager state)
    {
        if (state.CurrentPhase != GamePhase.Setup)
            return false;
            
        if (state.CurrentPlayerId != PlayerId)
            return false;
            
        if (TargetPlayerId == PlayerId)
            return false;
            
        if (string.IsNullOrEmpty(state.SelectedActorCardId))
            return false;
            
        var targetState = state.GetPlayerState(TargetPlayerId);
        return targetState != null && string.IsNullOrEmpty(targetState.AssignedActorCardId);
    }
    
    public override void Execute(GameStateManager state)
    {
        state.ConfirmActorAssignment(PlayerId, TargetPlayerId);
    }
}