using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GM_SetupPhase : GM_BasePhase
{
    public override GamePhase PhaseType => GamePhase.Setup;
    
    // Roll tracking
    private List<Player> _playersToRoll = new List<Player>();
    private Dictionary<Player, int> _rolledPlayers = new Dictionary<Player, int>();
    
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

    private void BeginRerollStage()
    {
        Debug.Log($"Players tied for highest roll will reroll: {_playersToRoll.GetPlayerIDList()}");
        game.currentPlayerIndex = game.players.IndexOf(_playersToRoll[0]);
        StartPlayerTurn();
    }
    
    #endregion

    #region Roll Stage
    
    public override void PlayerRolledDice(int roll)
    {
        _rolledPlayers.Add(game.CurrentPlayer, roll);
        _playersToRoll.Remove(game.CurrentPlayer);
        
        Debug.Log($"Player {game.CurrentPlayer.playerID} rolled {roll}");
        
        if (AllPlayersHaveRolled())
        {
            EndPlayerTurn();
            ProcessRollResults();
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

    private void ProcessRollResults()
    {
        Debug.Log("=== Processing roll results ===");
        
        OnAllPlayersRolled?.Invoke();
        
        //TODO: ui work
        int highestRoll = _rolledPlayers.Values.Max();
        List<Player> winnersOfRoll = GetPlayersWithRoll(highestRoll);
        
        if (winnersOfRoll.Count == 1)
        {
            //TODO: ui work
            Debug.Log($"Player {winnersOfRoll[0].playerID} won with roll {highestRoll}");
            
            HandleUniqueWinner(winnersOfRoll[0]);
        }
        else
        {
            //TODO: ui work
            Debug.Log($"Roll {highestRoll} is tied between: {winnersOfRoll.GetPlayerIDList()}");
            HandleTiedRoll(winnersOfRoll);
        }
    }

    private List<Player> GetPlayersWithRoll(int rollValue)
    {
        return _rolledPlayers
            .Where(pair => pair.Value == rollValue)
            .Select(pair => pair.Key)
            .ToList();
    }

    private void HandleUniqueWinner(Player winner)
    {
        //TODO: ui work
        _playerToSelect = winner;
        _rolledPlayers.Clear();

        CurrentStage = SetupStage.AssignActor;
        
    }

    private void HandleTiedRoll(List<Player> tiedPlayers)
    {
        //TODO: ui work
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

    public override void StartPlayerTurn()
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

    public override void EndPlayerTurn()
    {
        Player current = game.CurrentPlayer;
        
        base.EndPlayerTurn();
        
        Debug.Log($"Player {current.playerID} turn ended");
    }

    public override void MoveToNextPlayer()
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
            AutoAssignLastActor();
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
        //TODO: add an event for this and invoke it in ui and listen in autoassignlastactor
        return _unassignedActors.Count == 1;
    }
    
    private void AutoAssignLastActor()
    {
        // Notify UI to update visuals

        var lastPlayer = _unassignedPlayers[0];
        var lastActor = _unassignedActors[0];
        //TODO: ui
        AssignActorToPlayer(lastPlayer, lastActor);
        
        OnLastActorAssigned?.Invoke(lastPlayer, lastActor);
        
        OnAllActorsAssigned();
    }
    
    private void OnAllActorsAssigned()
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
    AssignActor
}

