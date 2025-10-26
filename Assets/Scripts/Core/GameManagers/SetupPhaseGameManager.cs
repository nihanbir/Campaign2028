using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SetupPhaseGameManager : MonoBehaviour
{
    public static SetupPhaseGameManager Instance;
    
    [Header("Decks")] 
    public GameDeckSO gameDeckData;
    [HideInInspector] public List<ActorCard> actorDeck;
    
    [Header("Game State")] 
    public int maxPlayers = 6;
    public List<Player> players;
    public int currentPlayerIndex = 0;
    
    public Player CurrentPlayer => players[currentPlayerIndex];
    
    // Roll tracking
    private List<Player> playersToRoll;
    private Dictionary<Player, int> rolledPlayers = new Dictionary<Player, int>();
    
    // Actor assignment tracking
    private Player playerToSelect;

    [HideInInspector] public GamePhase currentGamePhase = GamePhase.Setup;
    
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
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        players = new List<Player>(FindObjectsOfType<Player>());
    }

    void Start()
    {
        InitializeGame();
    }

    public void InitializeGame()
    {
        LoadDecks();
        SetupPhaseUIManager.Instance.InitializePhaseUI();
        BeginRollStage();
    }

    private void LoadDecks()
    {
        if (gameDeckData != null)
        {
            actorDeck = gameDeckData.GetActorDeck();
        }
        else
        {
            Debug.LogError("GameDeckSO not assigned! Please assign in inspector.");
        }
    }

    private void InitializeRollTracking()
    {
        playersToRoll = new List<Player>(players);
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
        currentPlayerIndex = 0;
        StartPlayerTurn();
    }

    private void BeginRerollStage()
    {
        Debug.Log($"Players tied for highest roll will reroll: {GetPlayerIDList(playersToRoll)}");
        currentPlayerIndex = players.IndexOf(playersToRoll[0]);
        StartPlayerTurn();
    }

    private void BeginAssignActorStage()
    {
        currentPlayerIndex = players.IndexOf(playerToSelect);
        StartPlayerTurn();
    }

    #endregion

    #region Turn Management

    public void StartPlayerTurn()
    {
        Player current = CurrentPlayer;
        Debug.Log($"Player {current.playerID} turn started - Stage: {CurrentStage}");

        SetupPhaseUIManager.Instance.OnPlayerTurnStarted(current);
        
        if (SetupPhaseAIManager.Instance.IsAIPlayer(current))
        {
            var aiPlayer = SetupPhaseAIManager.Instance.GetAIPlayer(current);
            StartCoroutine(SetupPhaseAIManager.Instance.ExecuteAITurn(aiPlayer));
        }
    }

    public void EndPlayerTurn()
    {
        Player current = CurrentPlayer;
        SetupPhaseUIManager.Instance.OnplayerTurnEnded(current);
        Debug.Log($"Player {current.playerID} turn ended");
    }

    private void MoveToNextPlayer()
    {
        switch (CurrentStage)
        {
            case SetupStage.Roll:
                currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
                break;
            
            case SetupStage.Reroll:
                int currentRerollIndex = playersToRoll.IndexOf(CurrentPlayer);
                int nextRerollIndex = (currentRerollIndex + 1) % playersToRoll.Count;
                currentPlayerIndex = players.IndexOf(playersToRoll[nextRerollIndex]);
                break;
        }
        
        StartPlayerTurn();
    }

    #endregion

    #region Dice Rolling Logic

    public void PlayerRolledDice()
    {
        int roll = GameUIManager.Instance._diceRoll;
        rolledPlayers.Add(CurrentPlayer, roll);
        playersToRoll.Remove(CurrentPlayer);
        
        Debug.Log($"Player {CurrentPlayer.playerID} rolled {roll}");
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
            SetupPhaseUIManager.Instance.UpdateUIForPlayer(rolledPlayer.Key, true);
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
        
        playerToSelect = winner;
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

    public bool CanAssignActor(Player targetPlayer)
    {
        if (CurrentStage != SetupStage.AssignActor)
        {
            Debug.LogWarning("Not in actor assignment stage!");
            return false;
        }

        if (targetPlayer == CurrentPlayer)
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
        Debug.Log("Remaining actor count is: " + SetupPhaseUIManager.Instance.unassignedActorCards.Count);
        return SetupPhaseUIManager.Instance.unassignedActorCards.Count == 1;
    }

    private void AutoAssignLastActor()
    {
        // Notify UI to update visuals
        SetupPhaseUIManager.Instance.AutoAssignLastActor();
        
        OnAllActorsAssigned();
    }

    private void OnAllActorsAssigned()
    {
        Debug.Log("=== All actors assigned! Moving to Main Game Phase ===");
        // currentGamePhase = GamePhase.MainGame;
        // GameUIManager.Instance.UpdateGamePhase(GamePhase.MainGame);
        // SetupPhaseUIManager.Instance.OnSetupPhaseComplete();
    }

    #endregion

    #region Helper Methods

    private string GetPlayerIDList(List<Player> playerList)
    {
        return string.Join(", ", playerList.Select(p => p.playerID));
    }

    #endregion
}

public enum SetupStage
{
    Roll,
    Reroll,
    AssignActor
}

public enum GamePhase
{
    Setup,
    MainGame,
    CivilWar,
    GameOver
}