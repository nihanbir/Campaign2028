using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class GM_SetupPhase : GM_BasePhase
{
    public override GamePhase PhaseType => GamePhase.Setup;
    
    // Roll tracking
    private List<Player> _playersToRoll = new ();
    private Dictionary<Player, int> _rolledPlayers = new ();
    
    private List<ActorCard> _unassignedActors;
    private List<Player> _unassignedPlayers;
    
    public List<ActorCard> GetUnassignedActors() => _unassignedActors;
    public List<Player> GetUnassignedPlayers() => _unassignedPlayers;
    
    // Actor assignment tracking
    private Player _playerToSelect;
    private ActorCard _actorToAssign;

    public GM_SetupPhase()
    {
        _unassignedActors = new List<ActorCard>(game.actorDeck);
        _unassignedPlayers = new List<Player>(game.players);
        
    }
    
    private SetupStage _currentStage = SetupStage.None;
    
    public SetupStage CurrentStage
    {
        get => _currentStage;
        private set
        {
            if (_currentStage != value)
            {
                _currentStage = value;
                TransitionToStage(value);
            }
        }
    }

    public event Action OnAllPlayersRolled;
    public event Action OnActorAssignStage;
    public event Action<Player, ActorCard> OnLastActorAssigned;
    public event Action<List<Player>> OnTiedRoll;
    public event Action<Player> OnUniqueWinner;


    protected override void BeginPhase()
    {
        base.BeginPhase();

        var ui = GameUIManager.Instance.setupUI;
        if (ui)
        {
            ui.OnUIReady = () =>
            {
                Debug.Log("🟢 SetupPhase UI Ready — starting player turns");
                CurrentStage = SetupStage.Roll;
            };
        }
        else
        {
            // fallback in case UI not found
            CurrentStage = SetupStage.Roll;
        }
    }

    #region Stage Transitions

    private void TransitionToStage(SetupStage newStage)
    {
        Debug.Log($"=== Transitioning to {newStage} stage ===");
        
        switch (newStage)
        {
            case SetupStage.Roll:
                BeginRollStage();
                break;
            
            case SetupStage.Reroll:
                BeginRerollStage();
                break;
            
            case SetupStage.AssignActor:
                BeginAssignActorStage();
                break;
        }
    }
    
    private void BeginRollStage()
    {
        
        Debug.Log("All players will roll dice");
        InitializeRollTracking();
        
        game.currentPlayerIndex = 0;
        StartPlayerTurn();
    }
    
    private void InitializeRollTracking()
    {
        _playersToRoll.AddRange(game.players);
        _rolledPlayers.Clear();
    }

    public void BeginRerollStage()
    {
        Debug.Log($"Players tied for highest roll will reroll: {_playersToRoll.GetPlayerIDList()}");
        game.currentPlayerIndex = game.players.IndexOf(_playersToRoll[0]);
        StartPlayerTurn();
    }
    
    #endregion

    #region Roll Stage
    
    protected override void PlayerRolledDice(Player player, int roll)
    {
        _rolledPlayers.Add(player, roll);
        _playersToRoll.Remove(player);
        
        Debug.Log($"Player {player.playerID} rolled {roll}");
        
        if (AllPlayersHaveRolled())
        {
            EndPlayerTurn();
            OnAllPlayersRolled?.Invoke();
        }
        else
        {
            MoveToNextPlayer();
        }
    }

    private bool AllPlayersHaveRolled()
    {
        return _playersToRoll.Count == 0;
    }

    public void ProcessRollResults()
    {
        Debug.Log("=== Processing roll results ===");
        
        int highestRoll = _rolledPlayers.Values.Max();
        List<Player> winnersOfRoll = GetPlayersWithRoll(highestRoll);
        
        if (winnersOfRoll.Count == 1)
        {
            Debug.Log($"Player {winnersOfRoll[0].playerID} won with roll {highestRoll}");
            OnUniqueWinner?.Invoke(winnersOfRoll[0]);
        }
        else
        {
            Debug.Log($"Roll {highestRoll} is tied between: {winnersOfRoll.GetPlayerIDList()}");
            OnTiedRoll?.Invoke(winnersOfRoll);
        }
    }

    private List<Player> GetPlayersWithRoll(int rollValue)
    {
        return _rolledPlayers
            .Where(pair => pair.Value == rollValue)
            .Select(pair => pair.Key)
            .ToList();
    }

    public void HandleUniqueWinner(Player winner)
    {
        _playerToSelect = winner;
        _rolledPlayers.Clear();

        CurrentStage = SetupStage.AssignActor;
        
    }

    public void HandleTiedRoll(List<Player> tiedPlayers)
    {
        _playersToRoll.Clear();
        _playersToRoll.AddRange(tiedPlayers);
        _rolledPlayers.Clear();
        
        if (CurrentStage == SetupStage.Reroll)
        {
            MoveToNextPlayer();
        }
        else
        {
            CurrentStage = SetupStage.Reroll;
        }
    }

    #endregion
    
    private void BeginAssignActorStage()
    {
        game.currentPlayerIndex = game.players.IndexOf(_playerToSelect);
        
        OnActorAssignStage?.Invoke();

        StartPlayerTurn();
    }


    #region Turn Management

    protected override void StartPlayerTurn()
    {
        Player current = game.CurrentPlayer;
        Debug.Log($"Player {current.playerID} turn started - Stage: {CurrentStage}");
        
        base.StartPlayerTurn();
        
        if (AIManager.Instance.IsAIPlayer(current))
        {
            var aiPlayer = AIManager.Instance.GetAIPlayer(current);
            game.StartCoroutine(AIManager.Instance.setupAI.ExecuteAITurn(aiPlayer));
        }
    }

    protected override void EndPlayerTurn()
    {
        Player current = game.CurrentPlayer;
        
        base.EndPlayerTurn();
        
        Debug.Log($"Player {current.playerID} turn ended");
    }

    protected override void MoveToNextPlayer()
    {
        EndPlayerTurn();
        
        switch (CurrentStage)
        {
            case SetupStage.Roll:
                game.currentPlayerIndex = (game.currentPlayerIndex + 1) % game.players.Count;
                break;
            
            case SetupStage.Reroll:
                int currentRerollIndex = _playersToRoll.IndexOf(game.CurrentPlayer);
                int nextRerollIndex = (currentRerollIndex + 1) % _playersToRoll.Count;
                game.currentPlayerIndex = game.players.IndexOf(_playersToRoll[nextRerollIndex]);
                break;
        }
        
        StartPlayerTurn();
    }

    #endregion

    #region Actor Assignment Logic
    
    private bool CanAssignActor(Player targetPlayer)
    {
        if (CurrentStage != SetupStage.AssignActor)
        {
            Debug.LogWarning("Not in actor assignment stage!");
            return false;
        }
    
        if (targetPlayer == game.CurrentPlayer)
        {
            Debug.LogWarning("Can't assign an actor to yourself!");
            return false;
        }
    
        return true;
    }

   
    public bool TryAssignActorToPlayer(Player player, ActorCard actorToAssign)
    {
        if (!CanAssignActor(player))
        {
            return false;
        }
        
        AssignActorToPlayer(player, actorToAssign);

        EndPlayerTurn();
        
        //TODO: call from ui
        // Check if only one player remains without an actor
        if (ShouldAutoAssignLastActor())
        {
            DOVirtual.DelayedCall(0.8f, AutoAssignLastActor);
        }
        else
        {
            CurrentStage = SetupStage.Roll;
        }
        
        return true;
    }
    
    private void AssignActorToPlayer(Player player, ActorCard actorToAssign)
    {
        player.assignedActor = actorToAssign;
        Debug.Log($"Assigned {actorToAssign.cardName} to Player {player.playerID}");
        
        _unassignedActors.Remove(actorToAssign);
        _unassignedPlayers.Remove(player);
    }
    
    private bool ShouldAutoAssignLastActor()
    {
        return _unassignedActors.Count == 1;
    }
    
    private void AutoAssignLastActor()
    {
        var lastPlayer = _unassignedPlayers[0];
        var lastActor = _unassignedActors[0];

        AssignActorToPlayer(lastPlayer, lastActor);
        
        // Notify UI to update visuals
        OnLastActorAssigned?.Invoke(lastPlayer, lastActor);
        
        // OnAllActorsAssigned();
    }
    
    public void OnAllActorsAssigned()
    {
        Debug.Log("=== All actors assigned! Moving to Main Game Phase ===");
        //TODO: ui
        game.SetPhase(GameManager.Instance.mainPhase);
    }

    #endregion


}

public enum SetupStage
{
    None,
    Roll,
    Reroll,
    AssignActor,
    LastActorAssigned
}

/// ======= Event Bus & Payloads (lightweight, mobile-safe) =======

public readonly struct SetupStageEvent : IGameEvent
{
    public readonly SetupStage stage;
    public readonly object Payload; // keep generic for flexibility

    public SetupStageEvent(SetupStage stage, object payload)
    {
        this.stage = stage;
        Payload = payload;
    }
}