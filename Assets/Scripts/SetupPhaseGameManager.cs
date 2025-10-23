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
    private List<Player> playersToTakeTurn; // players who need to roll dice this round
    
    public Player CurrentPlayer => players[currentPlayerIndex];
    private bool canAssignActor = false;
    private Dictionary<Player, int> rolledPlayers = new Dictionary<Player, int>();
    private Player playerToSelect;

    public SetupStage currentStage = SetupStage.Roll;
    
    public GamePhase currentPhase = GamePhase.Setup;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        players = new List<Player>(FindObjectsOfType<Player>());
    }

    void Start()
    {
        InitializeGame();
        
        // Initially all players roll
        playersToTakeTurn = new List<Player>(players);
        
        StartTurn();
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
            return;
        }

        //TODO:Have this in a general game manager consistent between scenes or phases
        currentPhase = GamePhase.Setup;
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
            currentPlayerIndex = players.IndexOf(playersToTakeTurn[0]);
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
        rolledPlayers.Add(CurrentPlayer, GameUIManager.Instance._diceRoll);
        playersToTakeTurn.Remove(CurrentPlayer);
        
        if (playersToTakeTurn.Count == 0)
        {
            canAssignActor = IsHighestRollUnique();
            if (!canAssignActor)
            {
                EndTurn();
                currentPlayerIndex = 0;
            }
        }
        else
        {
            EndTurn();
        }
    }
    
    private bool IsHighestRollUnique()
    {
        playersToTakeTurn.Clear();
        // Group by dice roll value
        var rollGroups = rolledPlayers
            .GroupBy(pair => pair.Value)
            .ToDictionary(g => g.Key, g => g.ToList());
        
        // Find the highest roll value
        int highestRoll = rolledPlayers.Values.Max();

        // Find how many players have the highest roll
        var highestRollPlayers = rollGroups[highestRoll];

        if (highestRollPlayers.Count == 1)
        {
            // Highest roll is unique
            playerToSelect = highestRollPlayers[0].Key;
            currentStage = SetupStage.AssignActor;
            
            Debug.Log($"Highest roll {highestRoll} is unique. Player {playerToSelect.playerID} will select.");
            return true;
        }
       
        // Highest roll is tied, all players with highest roll must reroll
        foreach (var pair in highestRollPlayers)
        {
            if (!playersToTakeTurn.Contains(pair.Key))
                playersToTakeTurn.Add(pair.Key);
            
            if (rolledPlayers.Contains(pair))
                rolledPlayers.Remove(pair.Key);
        }

        currentStage = SetupStage.Reroll;
        Debug.Log($"Highest roll {highestRoll} is tied. Players tied: {string.Join(", ", highestRollPlayers.Select(p => p.Key.playerID))} will reroll.");
        return false;
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