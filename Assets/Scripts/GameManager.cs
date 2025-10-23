using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Decks")] 
    public GameDeckSO gameDeckData; // Assign in inspector
    [HideInInspector] public List<ActorCard> actorDeck;
    [HideInInspector] public List<AllegianceCard> allegianceDeck;
    [HideInInspector] public List<StateCard> stateDeck;
    [HideInInspector] public List<InstitutionCard> institutionDeck;
    [HideInInspector] public List<EventCard> eventDeck;
    
    [Header("Game State")] 
    public int maxPlayers = 6;
    public List<Player> players;
    public int currentPlayerIndex = 0;
    public Player CurrentPlayer => players[currentPlayerIndex];
    
    public Card currentTableCard; // State/Institution on table
    public bool isCardOnTable => currentTableCard != null;
    
    public GamePhase currentPhase = GamePhase.Setup;
    public int secededStatesCount = 0;
    public int totalEVsInGame = 549;
    
    // Discard piles
    private List<Card> stateDiscardPile;
    private List<EventCard> eventDiscardPile;
    
    
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
        // Load decks from ScriptableObject
        if (gameDeckData != null)
        {
            stateDeck = gameDeckData.GetStateDeck();
            institutionDeck = gameDeckData.GetInstitutionDeck();
            eventDeck = gameDeckData.GetEventDeck();
            actorDeck = gameDeckData.GetActorDeck();
            allegianceDeck = gameDeckData.GetAllegianceDeck();
        }
        else
        {
            Debug.LogError("GameDeckSO not assigned! Please assign in inspector.");
            return;
        }

        // Setup phase
        SetupPhase();
    }

    #region SetupPhase

    public void SetupPhase()
    {
        currentPhase = GamePhase.Setup;
    }
    
    #endregion
    
    public void StartTurn()
    {
        Player current = CurrentPlayer;
        Debug.Log($"Player {current.playerID} turn started.");

        // Notify UI manager to enable interaction for current player
        SetupPhaseUIManager.Instance.OnPlayerTurnStarted(current);
    }

    public void EndTurn()
    {
        Debug.Log($"Player {CurrentPlayer.playerID} turn ended.");

        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;

        StartTurn();
    }

}


public enum GamePhase
{
    Setup,
    MainGame,
    CivilWar,
    GameOver
}