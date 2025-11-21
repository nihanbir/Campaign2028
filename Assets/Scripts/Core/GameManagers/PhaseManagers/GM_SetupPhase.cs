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
    private List<AllegianceCard> _allegianceDeck;
    
    public List<ActorCard> GetUnassignedActors() => _unassignedActors;
    public List<Player> GetUnassignedPlayers() => _unassignedPlayers;
    
    // Actor assignment tracking
    private Player _playerToSelect;
    private ActorCard _selectedActor;

    public GM_SetupPhase()
    {
        _unassignedActors = new List<ActorCard>(game.actorDeck);
        _unassignedPlayers = new List<Player>(game.players);
        _allegianceDeck = new List<AllegianceCard>(game.allegianceDeck);
        _allegianceDeck.ShuffleInPlace(); // Shuffle allegiance deck
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

    // Helper to check if command system is enabled
    private bool UseCommandSystem => NetworkAdapter.Instance != null && NetworkAdapter.Instance.IsCommandSystemEnabled;

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

        TurnFlowBus.Instance.Raise(new SetupStageEvent(SetupStage.BeginPhase, new BeginPhaseData(_unassignedPlayers, _unassignedActors)));
        
        // If using command system, let GameStateManager handle setup initialization
        if (UseCommandSystem)
        {
            NetworkAdapter.Instance.StateManager.InitializeSetupPhase();
        }
        
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
        // If command system is enabled, it handles the roll via GameStateManager
        if (UseCommandSystem)
        {
            // Command system will raise the appropriate events
            return;
        }
        
        // Original logic for when command system is disabled
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
            
            TurnFlowBus.Instance.Raise(new SetupStageEvent(SetupStage.UniqueWinner, new UniqueWinnerData(winnersOfRoll[0])));
            
            HandleUniqueWinner(winnersOfRoll[0]);
        }
        else
        {
            Debug.Log($"Roll {highestRoll} is tied between: {winnersOfRoll.GetPlayerIDList()}");
            
            TurnFlowBus.Instance.Raise(new SetupStageEvent(SetupStage.TiedRoll, new TiedRollData(winnersOfRoll)));
            
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
            CurrentStage = SetupStage.Reroll;
        }
    }

    #endregion
    
    private void BeginAssignActorStage()
    {
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
            if (UseCommandSystem)
            {
                NetworkAdapter.Instance.RequestConfirmActorAssignment(game.CurrentPlayer.playerID, player.playerID);
            }
            else
            {
                TryAssignActorToPlayer(player, _selectedActor);
            }
        }
    }
    
    private void HandleCardHeldRequest(ActorCard actorCard)
    {
        if (_selectedActor == actorCard) return;
        
        _selectedActor = actorCard;
        
        Debug.Log($"Held actor: {_selectedActor.cardName}");
        
        // If using command system, notify it about the selection
        if (UseCommandSystem)
        {
            NetworkAdapter.Instance.RequestSelectActor(game.CurrentPlayer.playerID, actorCard);
        }
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
        
        AssignAllegianceToPlayer(player);
        
        TurnFlowBus.Instance.Raise(new SetupStageEvent(SetupStage.ActorAssigned, new ActorAssignedData(player, actorToAssign)));
    }
    
    private void AssignAllegianceToPlayer(Player player)
    {
        if (_allegianceDeck.Count == 0)
        {
            Debug.LogWarning("No allegiance cards remaining!");
            return;
        }
    
        AllegianceCard allegiance = _allegianceDeck.PopFront();
        player.assignedAllegiance = allegiance;
    
        Debug.Log($"Assigned {allegiance.allegiance} allegiance to Player {player.playerID}");
    
        // Notify UI about allegiance assignment
        TurnFlowBus.Instance.Raise(new SetupStageEvent(
            SetupStage.AllegianceAssigned, 
            new AllegianceAssignedData(player, allegiance)
        ));
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
        TurnFlowBus.Instance.Raise(new SetupStageEvent(SetupStage.LastActorAssigned));
        
        Debug.Log("=== All actors assigned! Moving to Main Game Phase ===");
        game.SetPhase(GameManager.Instance.mainPhase);
    }

    #endregion

    #region Command System Sync
    
    /// <summary>
    /// Called by GameStateManager when using command system to sync local state
    /// </summary>
    public void SyncFromStateManager(SetupStage stage, Player playerToSelect, List<Player> playersToRoll)
    {
        _currentStage = stage;
        _playerToSelect = playerToSelect;
        _playersToRoll.Clear();
        _playersToRoll.AddRange(playersToRoll);
    }
    
    /// <summary>
    /// Called by GameStateManager to sync roll results
    /// </summary>
    public void SyncRollResult(Player player, int roll)
    {
        _rolledPlayers[player] = roll;
        _playersToRoll.Remove(player);
    }
    
    /// <summary>
    /// Called by GameStateManager to sync actor assignment
    /// </summary>
    public void SyncActorAssignment(Player player, ActorCard actor)
    {
        _unassignedActors.Remove(actor);
        _unassignedPlayers.Remove(player);
        _selectedActor = null;
    }
    
    #endregion
}