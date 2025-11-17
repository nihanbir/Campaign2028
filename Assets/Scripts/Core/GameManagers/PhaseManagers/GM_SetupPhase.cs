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
    private ActorCard _selectedActor;

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

    protected override void HandleTurnEvent(IGameEvent e)
    {
        base.HandleTurnEvent(e);
        
        if (!isActive) return;
        
        if (e is CardInputEvent c)
        {
            switch (c.stage)
            {
                case CardInputStage.Clicked:
                    HandleCardClickedRequest(c);
                    break;
                
                case CardInputStage.Held:
                    HandleCardHeldRequest((ActorCard)c.payload);
                    break;
            }
        }
    }

    protected override void BeginPhase()
    {
        base.BeginPhase();

        Debug.Log("🟢 SetupPhase UI Ready — starting player turns");
        
        CurrentStage = SetupStage.Roll;
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
            
            case SetupStage.BeginActorAssignment:
                BeginAssignActorStage();
                break;
        }
    }
    
    private void BeginRollStage()
    {
        
        Debug.Log("All players will roll dice");
        InitializeRollTracking();
        
        TurnFlowBus.Instance.Raise(new SetupStageEvent(SetupStage.Roll));
        
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

    protected override void HandleRequestedRoll()
    {
        base.HandleRequestedRoll();
        
        _rolledPlayers.Add(game.CurrentPlayer, diceRoll);
        _playersToRoll.Remove(game.CurrentPlayer);
        
        Debug.Log($"Player {game.CurrentPlayer.playerID} rolled {diceRoll}");
        
        if (AllPlayersHaveRolled())
        {
            EndPlayerTurn();
            TurnFlowBus.Instance.Raise(new SetupStageEvent(SetupStage.AllPlayersRolled));
            
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
        
        int highestRoll = _rolledPlayers.Values.Max();
        List<Player> winnersOfRoll = GetPlayersWithRoll(highestRoll);
        
        if (winnersOfRoll.Count == 1)
        {
            Debug.Log($"Player {winnersOfRoll[0].playerID} won with roll {highestRoll}");
            
            TurnFlowBus.Instance.Raise(new SetupStageEvent(SetupStage.UniqueWinner, new UniqueWinner(winnersOfRoll[0])));
            
            HandleUniqueWinner(winnersOfRoll[0]);
        }
        else
        {
            Debug.Log($"Roll {highestRoll} is tied between: {winnersOfRoll.GetPlayerIDList()}");
            
            TurnFlowBus.Instance.Raise(new SetupStageEvent(SetupStage.TiedRoll, new TiedRoll(winnersOfRoll)));
            
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

    public void HandleUniqueWinner(Player winner)
    {
        _playerToSelect = winner;
        _rolledPlayers.Clear();

        //TODO: maybe don't change the stage here
        CurrentStage = SetupStage.BeginActorAssignment;
        
    }

    private void HandleTiedRoll(List<Player> tiedPlayers)
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
            //TODO: maybe don't change the stage here
            CurrentStage = SetupStage.Reroll;
        }
    }

    #endregion
    
    private void BeginAssignActorStage()
    {
        // game.currentPlayerIndex = 0;
        
        game.currentPlayerIndex = game.players.IndexOf(_playerToSelect);
        
        TurnFlowBus.Instance.Raise(new SetupStageEvent(SetupStage.BeginActorAssignment));
        
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

    private void HandleCardClickedRequest(CardInputEvent e)
    {
        if (e.payload is Player player)
        {
            TryAssignActorToPlayer(player, _selectedActor);
        }
    }
    
    private void HandleCardHeldRequest(ActorCard actorCard)
    {
        if (_selectedActor == actorCard) return;
        
        _selectedActor = actorCard;
        
        Debug.Log($"Held actor: {_selectedActor.cardName}");
    }
    
    private bool CanAssignActor(Player targetPlayer)
    {
        if (CurrentStage != SetupStage.BeginActorAssignment)
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

   
    private bool TryAssignActorToPlayer(Player player, ActorCard actorToAssign)
    {
        if (actorToAssign == null)
        {
            Debug.Log("actor to assign was null");
            
            return false;
        }
        if (!CanAssignActor(player))
        {
            Debug.Log("couldnt assign");
            return false;
        }
        
        AssignActorToPlayer(player, actorToAssign);

        EndPlayerTurn();
        
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
        _selectedActor = null;
        
        player.assignedActor = actorToAssign;
        Debug.Log($"Assigned {actorToAssign.cardName} to Player {player.playerID}");
        
        _unassignedActors.Remove(actorToAssign);
        _unassignedPlayers.Remove(player);
        
        TurnFlowBus.Instance.Raise(new SetupStageEvent(SetupStage.ActorAssigned, new ActorAssigned(player, actorToAssign)));
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
        
        OnAllActorsAssigned();
        
    }
    
    private void OnAllActorsAssigned()
    {
        // Notify UI to update visuals
        TurnFlowBus.Instance.Raise(new SetupStageEvent(SetupStage.LastActorAssigned));
        
        Debug.Log("=== All actors assigned! Moving to Main Game Phase ===");
        //TODO: maybe transition in gamemanager instead
        game.SetPhase(GameManager.Instance.mainPhase);
    }

    #endregion


}

