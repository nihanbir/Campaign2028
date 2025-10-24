using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SetupPhaseGameManager : MonoBehaviour
{
    public static SetupPhaseGameManager Instance;
    
    [Header("Decks")] 
    public GameDeckSO gameDeckData; // Assign in inspector
    [HideInInspector] public List<ActorCard> actorDeck;
    
    [Header("Game State")] 
    public int maxPlayers = 6;
    public List<Player> players;
    public int currentPlayerIndex = 0;
    private List<Player> playersToRoll; // players who need to roll dice this round
    
    public Player CurrentPlayer => players[currentPlayerIndex];
    private Dictionary<Player, int> _rolledPlayers = new Dictionary<Player, int>();
    private Player playerToSelect;

    private SetupStage _currentStage = SetupStage.Roll;
    public SetupStage currentStage
    {
        get => _currentStage;
        set
        {
            if (_currentStage != value)
            {
                _currentStage = value;
                OnSetupStageChanged?.Invoke(_currentStage);
            }
        }
    }
    
    public delegate void SetupStageChangedHandler(SetupStage newStage);
    public event SetupStageChangedHandler OnSetupStageChanged;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        players = new List<Player>(FindObjectsOfType<Player>());
    }

    void Start()
    {
        InitializeGame();
        
        OnSetupStageChanged += HandleSetupStageChanged;
        
        // Initially all players roll
        playersToRoll = new List<Player>(players);
        
    }

    public void InitializeGame()
    {
        // Load decks from ScriptableObject
        if (gameDeckData != null)
        {
            actorDeck = gameDeckData.GetActorDeck();
        }
        else
        {
            Debug.LogError("GameDeckSO not assigned! Please assign in inspector.");
        }
    }
    
    public void StartTurn()
    {
        Debug.Log($"Player {CurrentPlayer.playerID} turn started.");

        // Notify UI manager to enable interaction for current player   
        SetupPhaseUIManager.Instance.OnPlayerTurnStarted(CurrentPlayer);
        
        if (SetupPhaseAIManager.Instance.IsAIPlayer(CurrentPlayer))
        {
            var aiPlayer = SetupPhaseAIManager.Instance.GetAIPlayer(CurrentPlayer);
            StartCoroutine(SetupPhaseAIManager.Instance.ExecuteAITurn(aiPlayer));
        }
    }

    public void EndTurn()
    {
        if (currentStage == SetupStage.Reroll)
        {
            Debug.Log($"Player {CurrentPlayer.playerID} re-roll turn ended.");
            currentPlayerIndex = players.IndexOf(playersToRoll[0]);
        }
        else
        {
            Debug.Log($"Player {CurrentPlayer.playerID} turn ended.");
            currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        }
        StartTurn();
    }

    public void PlayerRolledDice()
    {
        _rolledPlayers.Add(CurrentPlayer, GameUIManager.Instance._diceRoll);
        playersToRoll.Remove(CurrentPlayer);
        
        if (playersToRoll.Count == 0)
        {
            IsHighestRollUnique();
        }
        else
        {
            EndTurn();
        }
    }
    
    private void IsHighestRollUnique()
    {
        // Group by dice roll value
        var rollGroups = _rolledPlayers
            .GroupBy(pair => pair.Value)
            .ToDictionary(g => g.Key, g => g.ToList());
        
        // Find the highest roll value
        int highestRoll = _rolledPlayers.Values.Max();
        
        // Find how many players have the highest roll
        var highestRollPlayers = rollGroups[highestRoll];
        
        foreach (var rolledPlayer in _rolledPlayers)
        {
            SetupPhaseUIManager.Instance.UpdateUIForPlayer(rolledPlayer.Key, true);
        }
        
        if (highestRollPlayers.Count == 1)
        {
            // Highest roll is unique
            playerToSelect = highestRollPlayers[0].Key;
            
            Debug.Log($"Highest roll {highestRoll} is unique. Player {playerToSelect.playerID} will select.");
            _rolledPlayers.Clear();
            currentStage = SetupStage.AssignActor;
            return;
        }
       
        // Highest roll is tied, all players with highest roll must reroll
        foreach (var pair in highestRollPlayers)
        {
            if (!playersToRoll.Contains(pair.Key))
                playersToRoll.Add(pair.Key);
            
        }
        _rolledPlayers.Clear();
        Debug.Log($"Highest roll {highestRoll} is tied. Players tied: {string.Join(", ", highestRollPlayers.Select(p => p.Key.playerID))} will reroll.");
        currentStage = SetupStage.Reroll;
        EndTurn();
    }
    private void HandleSetupStageChanged(SetupStage newStage)
    {
        switch (newStage)
        {
            case SetupStage.Roll:
                Debug.Log("Stage changed to Roll: All players roll dice.");
                
                break;
            case SetupStage.Reroll:
                Debug.Log("Stage changed to Reroll: Players tied reroll dice.");
                
                break;
            case SetupStage.AssignActor:
                Debug.Log("Stage changed to AssignActor: Player selects an actor.");
                currentPlayerIndex = players.IndexOf(playerToSelect);
                StartTurn();
                break;
        }
    }
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