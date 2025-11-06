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
        _unassignedActors = game.actorDeck;
        _unassignedPlayers = game.players;
        
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

    // public event Action<Player, PlayerDisplayCard> OnActorAssignedToPlayer; 


    protected override void BeginPhase()
    {

        CurrentStage = SetupStage.Roll;
    }

    protected override void EndPhase()
    {
        // game.CurrentPhase = GamePhase.MainGame;
        
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
    #endregion

    #region Roll Stage
    private void BeginRollStage()
    {
        // PlayerDisplayCard.OnPlayerCardClicked -= AssignActorToPlayer;
        
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
    
    public override void PlayerRolledDice()
    {
        //TODO: handle this with an event from ui
        int roll = GameUIManager.Instance.DiceRoll;
        _rolledPlayers.Add(game.CurrentPlayer, roll);
        _playersToRoll.Remove(game.CurrentPlayer);
        
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
        return _playersToRoll.Count == 0;
    }
    
    public void ProcessRollResults()
    {
        Debug.Log("=== Processing roll results ===");
        
        //TODO: ui work
        // Show all dice results
        foreach (var rolledPlayer in _rolledPlayers)
        {
            rolledPlayer.Key.PlayerDisplayCard.ShowDice(false);
        }
        
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

        CurrentStage = SetupStage.Reroll;
        
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
        // PlayerDisplayCard.OnCardSelected += OnActorSelected;
        // PlayerDisplayCard.OnPlayerCardClicked += AssignActorToPlayer;
        game.currentPlayerIndex = game.players.IndexOf(_playerToSelect);

        StartPlayerTurn();
    }


    #region Turn Management

    public override void StartPlayerTurn()
    {
        Player current = game.CurrentPlayer;
        Debug.Log($"Player {current.playerID} turn started - Stage: {CurrentStage}");

        //TODO: event for ui
        GameUIManager.Instance.umSetupUI.OnPlayerTurnStarted(current);
        
        if (AIManager.Instance.IsAIPlayer(current))
        {
            var aiPlayer = AIManager.Instance.GetAIPlayer(current);
            game.StartCoroutine(AIManager.Instance.setupAI.ExecuteAITurn(aiPlayer));
        }
    }

    public override void EndPlayerTurn()
    {
        Player current = game.CurrentPlayer;
        
        //TODO: event for ui
        GameUIManager.Instance.umSetupUI.OnplayerTurnEnded(current);
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
                int currentRerollIndex = _playersToRoll.IndexOf(game.CurrentPlayer);
                int nextRerollIndex = (currentRerollIndex + 1) % _playersToRoll.Count;
                game.currentPlayerIndex = game.players.IndexOf(_playersToRoll[nextRerollIndex]);
                break;
        }
        
        StartPlayerTurn();
    }

    #endregion

    #region Actor Assignment Logic

    public void OnActorSelected(ISelectableDisplayCard card)
    {
        var playerCard = card as PlayerDisplayCard;
        if (!playerCard) return;
        _actorToAssign = playerCard.GetCard();
    }
    
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
    
    public void AssignActorToPlayer(PlayerDisplayCard displayCard)
    {
        var targetPlayer = displayCard.owningPlayer;
        
        if (!CanAssignActor(targetPlayer))
        {
            return;
        }
        
        targetPlayer.assignedActor = _actorToAssign;
        Debug.Log($"Assigned {_actorToAssign.cardName} to Player {targetPlayer.playerID}");
        
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
        
        //TODO:
        // OnActorAssignedToPlayer?.Invoke(displayCard);        
    }
    
    private bool ShouldAutoAssignLastActor()
    {
        //TODO: add an event for this and invoke it in ui and listen in autoassignlastactor
        // Debug.Log("Remaining actor count is: " + GameUIManager.Instance.setupUI.unassignedActorCards.Count);
        // return GameUIManager.Instance.setupUI.unassignedActorCards.Count == 1;
        return false;
    }
    
    private void AutoAssignLastActor()
    {
        // Notify UI to update visuals
        //TODO: ui
        // GameUIManager.Instance.setupUI.AutoAssignLastActor();
        
        OnAllActorsAssigned();
    }
    
    private void OnAllActorsAssigned()
    {
        Debug.Log("=== All actors assigned! Moving to Main Game Phase ===");
        //TODO: ui
       EndPhase();
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

