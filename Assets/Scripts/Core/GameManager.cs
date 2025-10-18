using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    [Header("Decks")]
    public GameDeckSO gameDeckData; // Assign in inspector
    
    private List<StateCard> stateDeck = new List<StateCard>();
    private List<InstitutionCard> institutionDeck = new List<InstitutionCard>();
    private List<EventCard> eventDeck = new List<EventCard>();
    private List<ActorCard> actorDeck = new List<ActorCard>();
    private List<AllegianceCard> allegianceDeck = new List<AllegianceCard>();
    
    [Header("Game State")]
    public List<Player> players = new List<Player>();
    public int currentPlayerIndex = 0;
    public Player CurrentPlayer => players[currentPlayerIndex];
    
    public Card currentTableCard; // State/Institution on table
    public bool isCardOnTable => currentTableCard != null;
    
    public GamePhase currentPhase = GamePhase.Setup;
    public int secededStatesCount = 0;
    public int totalEVsInGame = 549;
    
    // Discard piles
    private List<Card> stateDiscardPile = new List<Card>();
    private List<EventCard> eventDiscardPile = new List<EventCard>();
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    void Start()
    {
        InitializeGame();
    }
    
    public void InitializeGame()
    {
        Debug.Log("=== CAMPAIGN 2028 - Game Start ===");
        
        // Load decks from ScriptableObject
        if (gameDeckData != null)
        {
            stateDeck = gameDeckData.GetStateDeck();
            institutionDeck = gameDeckData.GetInstitutionDeck();
            eventDeck = gameDeckData.GetEventDeck();
            actorDeck = gameDeckData.GetActorDeck();
            allegianceDeck = gameDeckData.GetAllegianceDeck();
            
            Debug.Log($"Loaded: {stateDeck.Count} states, {institutionDeck.Count} institutions, {eventDeck.Count} events");
        }
        else
        {
            Debug.LogError("GameDeckSO not assigned! Please assign in inspector.");
            return;
        }
        
        // Combine State and Institution decks
        List<Card> combinedDeck = new List<Card>();
        combinedDeck.AddRange(stateDeck);
        combinedDeck.AddRange(institutionDeck);
        ShuffleDeck(combinedDeck);
        
        // Separate back after shuffle
        stateDeck.Clear();
        institutionDeck.Clear();
        foreach (var card in combinedDeck)
        {
            if (card is StateCard) stateDeck.Add((StateCard)card);
            else if (card is InstitutionCard) institutionDeck.Add((InstitutionCard)card);
        }
        
        ShuffleDeck(eventDeck);
        
        // Setup phase
        StartCoroutine(SetupPhase());
    }
    
    IEnumerator SetupPhase()
    {
        currentPhase = GamePhase.Setup;
        
        // Assign actors
        yield return StartCoroutine(AssignActors());
        
        // Assign allegiances
        ShuffleDeck(allegianceDeck);
        for (int i = 0; i < players.Count; i++)
        {
            players[i].allegiance = allegianceDeck[i];
            Debug.Log($"{players[i].playerName} is secretly aligned with {players[i].allegiance.allegiance}");
        }
        
        // Determine starting player (highest roller)
        int highestRoll = 0;
        int startingPlayerIndex = 0;
        for (int i = 0; i < players.Count; i++)
        {
            int roll = Random.Range(1, 7);
            Debug.Log($"{players[i].playerName} rolled: {roll}");
            if (roll > highestRoll)
            {
                highestRoll = roll;
                startingPlayerIndex = i;
            }
        }
        
        currentPlayerIndex = startingPlayerIndex;
        Debug.Log($"Starting player: {CurrentPlayer.playerName}");
        
        currentPhase = GamePhase.MainGame;
        StartTurn();
    }
    
    IEnumerator AssignActors()
    {
        // Roll for actor assignment order
        List<Player> rollOrder = new List<Player>(players);
        rollOrder = rollOrder.OrderByDescending(p => Random.Range(1, 7)).ToList();
        
        List<ActorCard> availableActors = new List<ActorCard>(actorDeck);
        
        foreach (var player in rollOrder)
        {
            // Each player assigns an actor to another player
            if (availableActors.Count == 0) break;
            
            int randomIndex = Random.Range(0, availableActors.Count);
            ActorCard chosenActor = availableActors[randomIndex];
            
            // Assign to a random other player who doesn't have an actor
            Player targetPlayer = players.FirstOrDefault(p => p.assignedActor == null);
            if (targetPlayer != null)
            {
                targetPlayer.assignedActor = chosenActor;
                availableActors.RemoveAt(randomIndex);
                Debug.Log($"{targetPlayer.playerName} assigned actor: {chosenActor.cardName}");
            }
        }
        
        yield return null;
    }
    
    public void StartTurn()
    {
        if (currentPhase != GamePhase.MainGame) return;
        
        Debug.Log($"\n--- {CurrentPlayer.playerName}'s Turn ---");
        
        // Step 1: Draw State/Institution (if none on table)
        if (!isCardOnTable)
        {
            DrawStateOrInstitution();
        }
        
        // Step 2: Draw Event
        DrawEvent();
        
        // Step 3: Roll the die (player action needed - will be called separately)
        Debug.Log("Waiting for player to roll...");
    }
    
    void DrawStateOrInstitution()
    {
        List<Card> combined = new List<Card>();
        combined.AddRange(stateDeck.Cast<Card>());
        combined.AddRange(institutionDeck.Cast<Card>());
        
        if (combined.Count == 0)
        {
            Debug.Log("No more State/Institution cards!");
            CheckCivilWarTrigger();
            return;
        }
        
        currentTableCard = combined[0];
        
        if (currentTableCard is StateCard)
            stateDeck.RemoveAt(0);
        else
            institutionDeck.RemoveAt(0);
        
        Debug.Log($"Drew: {currentTableCard.cardName}");
    }
    
    void DrawEvent()
    {
        if (eventDeck.Count == 0)
        {
            // Reshuffle discard pile
            eventDeck = new List<EventCard>(eventDiscardPile);
            eventDiscardPile.Clear();
            ShuffleDeck(eventDeck);
            Debug.Log("Reshuffled Event deck");
        }
        
        if (eventDeck.Count > 0)
        {
            EventCard drawnEvent = eventDeck[0];
            eventDeck.RemoveAt(0);
            Debug.Log($"Event drawn: {drawnEvent.cardName}");
            
            if (drawnEvent.mustPlayImmediately)
            {
                ApplyEvent(drawnEvent);
                eventDiscardPile.Add(drawnEvent);
            }
            else if (drawnEvent.canSave)
            {
                // Player decision: hold or discard
                // For now, auto-hold
                CurrentPlayer.HoldEvent(drawnEvent);
            }
        }
    }
    
    public void RollDie()
    {
        int roll = Random.Range(1, 7);
        Debug.Log($"{CurrentPlayer.playerName} rolled: {roll}");
        
        bool success = false;
        
        if (currentTableCard is StateCard state)
        {
            success = state.IsSuccessfulRoll(roll, CurrentPlayer.GetTeam());
            
            if (success)
            {
                CurrentPlayer.CaptureState(state);
                currentTableCard = null;
                
                if (state.hasRollAgain)
                {
                    Debug.Log("Roll Again! Take another turn.");
                    StartTurn();
                    return;
                }
            }
            else if (state.hasSecession && roll == 1)
            {
                Debug.Log($"{state.cardName} has SECEDED!");
                HandleSecession(state);
                currentTableCard = null;
            }
        }
        else if (currentTableCard is InstitutionCard institution)
        {
            success = institution.IsSuccessfulRoll(roll, CurrentPlayer.GetTeam());
            
            if (success)
            {
                CurrentPlayer.CaptureInstitution(institution);
                currentTableCard = null;
            }
        }
        
        if (!success && currentTableCard != null)
        {
            Debug.Log("Failed to capture. Card remains on table.");
        }
        
        EndTurn();
    }
    
    void HandleSecession(StateCard state)
    {
        secededStatesCount++;
        totalEVsInGame -= state.electoralVotes;
        stateDiscardPile.Add(state);
        
        // Remove from any player who had it
        foreach (var player in players)
        {
            player.RemoveState(state);
        }
        
        CheckCivilWarTrigger();
    }
    
    void CheckCivilWarTrigger()
    {
        // Trigger 1: 5+ states seceded
        if (secededStatesCount >= 5)
        {
            Debug.Log("⚔️ CIVIL WAR triggered by secessions!");
            StartCivilWar();
            return;
        }
        
        // Trigger 2: 290 EVs mathematically impossible
        if (totalEVsInGame < 290)
        {
            Debug.Log("⚔️ CIVIL WAR triggered - 290 EVs impossible!");
            StartCivilWar();
        }
    }
    
    void StartCivilWar()
    {
        currentPhase = GamePhase.CivilWar;
        StartCoroutine(CivilWarPhase());
    }
    
    IEnumerator CivilWarPhase()
    {
        Debug.Log("\n=== CIVIL WAR PHASE ===");
        
        // Roll initiative
        foreach (var player in players)
        {
            if (!player.isEliminated)
                player.RollInitiative();
        }
        
        // Sort by initiative
        List<Player> turnOrder = players.Where(p => !p.isEliminated)
                                        .OrderByDescending(p => p.initiativeRoll)
                                        .ToList();
        
        int round = 1;
        
        while (turnOrder.Count(p => !p.isEliminated) > 1)
        {
            Debug.Log($"\n--- Civil War Round {round} ---");
            
            foreach (var attacker in turnOrder)
            {
                if (attacker.isEliminated) continue;
                
                // Choose random target
                List<Player> validTargets = turnOrder.Where(p => !p.isEliminated && p != attacker).ToList();
                if (validTargets.Count > 0)
                {
                    Player target = validTargets[Random.Range(0, validTargets.Count)];
                    attacker.Attack(target);
                }
            }
            
            // Check for China agent win after round 1
            if (round == 1)
            {
                Player chinaPlayer = players.FirstOrDefault(p => 
                    !p.isEliminated && 
                    p.allegiance.allegiance == AllegianceType.China &&
                    p.heldEvent != null && 
                    p.heldEvent.eventType == EventType.ChineseAgent);
                
                if (chinaPlayer != null)
                {
                    Debug.Log($"🎉 {chinaPlayer.playerName} (China) WINS by surviving Round 1 with Chinese Agent!");
                    currentPhase = GamePhase.GameOver;
                    yield break;
                }
            }
            
            round++;
            yield return new WaitForSeconds(1f);
        }
        
        // Determine winner
        Player winner = turnOrder.FirstOrDefault(p => !p.isEliminated);
        if (winner != null)
        {
            Debug.Log($"🎉 {winner.playerName} WINS the Civil War!");
        }
        
        currentPhase = GamePhase.GameOver;
    }
    
    void ApplyEvent(EventCard eventCard)
    {
        Debug.Log($"Applying event: {eventCard.cardName}");
        // Event logic goes here based on eventCard.eventType
    }
    
    void EndTurn()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        StartTurn();
    }
    
    void ShuffleDeck<T>(List<T> deck)
    {
        for (int i = 0; i < deck.Count; i++)
        {
            T temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }
}

public enum GamePhase
{
    Setup,
    MainGame,
    CivilWar,
    GameOver
}