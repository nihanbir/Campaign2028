using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SetupPhaseGameManager : BasePhaseGameManager
{
    public SetupPhaseGameManager(GameManager gm) : base(gm) { }
    
    // Roll tracking
    private List<Player> playersToRoll = new List<Player>();
    private Dictionary<Player, int> rolledPlayers = new Dictionary<Player, int>();
    
    // Actor assignment tracking
    private Player _playerToSelect;
    
    private SetupStage _currentStage = SetupStage.Roll;
    
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

    public override void InitializePhase()
    {
        Debug.Log("Begin Roll");
        BeginRollStage();
    }

    private void InitializeRollTracking()
    {
        playersToRoll.AddRange(game.players);
        rolledPlayers.Clear();
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

    private void BeginRerollStage()
    {
        Debug.Log($"Players tied for highest roll will reroll: {GetPlayerIDList(playersToRoll)}");
        game.currentPlayerIndex = game.players.IndexOf(playersToRoll[0]);
        StartPlayerTurn();
    }

    private void BeginAssignActorStage()
    {
        game.currentPlayerIndex = game.players.IndexOf(_playerToSelect);
        StartPlayerTurn();
    }

    #endregion

    #region Turn Management

    public override void StartPlayerTurn()
    {
        Player current = game.CurrentPlayer;
        Debug.Log($"Player {current.playerID} turn started - Stage: {CurrentStage}");

        GameUIManager.Instance.setupUI.OnPlayerTurnStarted(current);
        
        if (AIManager.Instance.IsAIPlayer(current))
        {
            var aiPlayer = AIManager.Instance.GetAIPlayer(current);
            game.StartCoroutine(AIManager.Instance.setupAI.ExecuteAITurn(aiPlayer));
        }
    }

    public override void EndPlayerTurn()
    {
        Player current = game.CurrentPlayer;
        GameUIManager.Instance.setupUI.OnplayerTurnEnded(current);
        Debug.Log($"Player {current.playerID} turn ended");
    }

    public override void MoveToNextPlayer()
    {
        switch (CurrentStage)
        {
            case SetupStage.Roll:
                game.currentPlayerIndex = (game.currentPlayerIndex + 1) % game.players.Count;
                break;
            
            case SetupStage.Reroll:
                int currentRerollIndex = playersToRoll.IndexOf(game.CurrentPlayer);
                int nextRerollIndex = (currentRerollIndex + 1) % playersToRoll.Count;
                game.currentPlayerIndex = game.players.IndexOf(playersToRoll[nextRerollIndex]);
                break;
        }
        
        StartPlayerTurn();
    }

    #endregion

    #region Dice Rolling Logic

    public override void PlayerRolledDice()
    {
        int roll = GameUIManager.Instance.DiceRoll;
        rolledPlayers.Add(game.CurrentPlayer, roll);
        playersToRoll.Remove(game.CurrentPlayer);
        
        Debug.Log($"Player {game.CurrentPlayer.playerID} rolled {roll}");
        EndPlayerTurn();
        
        if (AllPlayersHaveRolled())
        {
            ProcessRollResults();
        }
        else
        {
            MoveToNextPlayer();
        }
    }

    private bool AllPlayersHaveRolled()
    {
        return playersToRoll.Count == 0;
    }

    private void ProcessRollResults()
    {
        Debug.Log("=== Processing roll results ===");
        
        // Show all dice results
        foreach (var rolledPlayer in rolledPlayers)
        {
            rolledPlayer.Key.PlayerDisplayCard.ShowDice(false);
        }
        
        int highestRoll = rolledPlayers.Values.Max();
        List<Player> winnersOfRoll = GetPlayersWithRoll(highestRoll);
        
        if (winnersOfRoll.Count == 1)
        {
            Debug.Log($"Player {winnersOfRoll[0].playerID} won with roll {highestRoll}");
            HandleUniqueWinner(winnersOfRoll[0]);
        }
        else
        {
            Debug.Log($"Roll {highestRoll} is tied between: {GetPlayerIDList(winnersOfRoll)}");
            HandleTiedRoll(winnersOfRoll);
        }
    }

    private List<Player> GetPlayersWithRoll(int rollValue)
    {
        return rolledPlayers
            .Where(pair => pair.Value == rollValue)
            .Select(pair => pair.Key)
            .ToList();
    }

    private void HandleUniqueWinner(Player winner)
    {
        
        _playerToSelect = winner;
        rolledPlayers.Clear();

        CurrentStage = SetupStage.AssignActor;
        
    }

    private void HandleTiedRoll(List<Player> tiedPlayers)
    {
        
        playersToRoll.Clear();
        playersToRoll.AddRange(tiedPlayers);
        rolledPlayers.Clear();

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

    public void AssignActorToPlayer(Player targetPlayer, ActorCard actor)
    {
        if (!CanAssignActor(targetPlayer))
        {
            return;
        }

        targetPlayer.assignedActor = actor;
        Debug.Log($"Assigned {actor.cardName} to Player {targetPlayer.playerID}");
        
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
    }

    private bool ShouldAutoAssignLastActor()
    {
        Debug.Log("Remaining actor count is: " + GameUIManager.Instance.setupUI.unassignedActorCards.Count);
        return GameUIManager.Instance.setupUI.unassignedActorCards.Count == 1;
    }

    private void AutoAssignLastActor()
    {
        // Notify UI to update visuals
        GameUIManager.Instance.setupUI.AutoAssignLastActor();
        
        OnAllActorsAssigned();
    }

    private void OnAllActorsAssigned()
    {
        Debug.Log("=== All actors assigned! Moving to Main Game Phase ===");
        game.CurrentPhase = GamePhase.MainGame;
       
    }

    #endregion

    #region Helper Methods

    private string GetPlayerIDList(List<Player> playerList)
    {
        return string.Join(", ", playerList.Select(p => p.playerID));
    }

    #endregion

    public void ClearPhase()
    {
        playersToRoll.Clear();
        _playerToSelect = null;
        rolledPlayers.Clear();
    }
}

public enum SetupStage
{
    Roll,
    Reroll,
    AssignActor
}

