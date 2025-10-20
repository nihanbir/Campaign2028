using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Manages all AI players in the game
public class AIManager : MonoBehaviour
{
    public static AIManager Instance;
    
    [Header("AI Configuration")]
    public int numberOfAIPlayers = 5;
    public bool mixPersonalities = true;
    public GameObject aiPlayerPrefab;
    
    [Header("AI Names")]
    public List<string> aiNames = new List<string>
    {
        "Agent Red", "Commander Blue", "Senator Smith", 
        "Governor Chen", "Mayor Johnson", "Director Park"
    };
    
    private List<AIPlayer> aiPlayers = new List<AIPlayer>();
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    // Create AI players for the game
    public void CreateAIPlayers(int humanPlayerCount)
    {
        int aiCount = numberOfAIPlayers - humanPlayerCount;
        aiCount = Mathf.Clamp(aiCount, 1, 5); // 1-5 AI players
        
        for (int i = 0; i < aiCount; i++)
        {
            CreateAIPlayer(i);
        }
        
        Debug.Log($"Created {aiCount} AI players");
    }
    
    void CreateAIPlayer(int index)
    {
        GameObject aiObj = aiPlayerPrefab != null ? 
            Instantiate(aiPlayerPrefab, transform) : 
            new GameObject($"AI_Player_{index}");
            
        AIPlayer ai = aiObj.GetComponent<AIPlayer>();
        if (ai == null) ai = aiObj.AddComponent<AIPlayer>();
        
        // Configure AI
        ai.playerName = index < aiNames.Count ? aiNames[index] : $"AI Player {index + 1}";
        ai.playerID = 100 + index; // AI IDs start at 100
        
        aiPlayers.Add(ai);
        
        // Register with game manager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.players.Add(ai);
        }
        
    }
    
    // Check if player is AI
    public bool IsAIPlayer(Player player)
    {
        return player is AIPlayer;
    }
    
    // Get AI player
    public AIPlayer GetAIPlayer(Player player)
    {
        return player as AIPlayer;
    }
    
    // Execute AI turn
    public IEnumerator ExecuteAITurn(Player player, Card currentCard, EventCard drawnEvent)
    {
        AIPlayer ai = GetAIPlayer(player);
        if (ai == null)
        {
            Debug.LogWarning("Tried to execute AI turn for non-AI player");
            yield break;
        }
        
        yield return StartCoroutine(ai.TakeTurn(currentCard, drawnEvent));
    }
    
    // AI coalition invitation
    public void ProcessCoalitionInvitation(Player inviter, Player invitee)
    {
        AIPlayer aiInvitee = GetAIPlayer(invitee);
        if (aiInvitee == null) return;
        
        bool accepted = aiInvitee.DecideJoinCoalition(inviter);
        
        if (accepted)
        {
            // Form coalition logic
            Debug.Log($"Coalition formed: {inviter.playerName} + {invitee.playerName}");
        }
    }
    
    // AI attack target selection for Civil War
    public Player SelectAIAttackTarget(AIPlayer attacker, List<Player> availablePlayers)
    {
        return attacker.SelectAttackTarget(availablePlayers);
    }
}

// Specialized AI personalities with unique behaviors
public class AggressiveAI : AIPlayer
{
    protected override bool DecideEventUsage(EventCard eventCard, Card targetCard)
    {
        // Always use events aggressively
        if (eventCard.eventType == EventType.Challenge) return true;
        if (eventCard.eventType == EventType.ExtraRoll) return true;
        
        return base.DecideEventUsage(eventCard, targetCard);
    }
    
    protected override float EvaluateEventValue(EventCard eventCard, Card targetCard)
    {
        float value = base.EvaluateEventValue(eventCard, targetCard);
        
        // Boost all event values - aggressive AI uses events more
        return Mathf.Min(1f, value + 0.2f);
    }
}

public class ConservativeAI : AIPlayer
{
    protected override bool DecideEventUsage(EventCard eventCard, Card targetCard)
    {
        // Save events unless forced to use
        if (eventCard.mustPlayImmediately) return true;
        if (eventCard.canSave && !IsHoldingEvent) return false; // Always save
        
        return base.DecideEventUsage(eventCard, targetCard);
    }
    
    protected override Player SelectStrategicTarget(List<Player> availablePlayers)
    {
        // Target weakest player (defensive strategy)
        return availablePlayers.OrderBy(p => GetPlayerStrength(p)).First();
    }
}

public class StrategicAI : AIPlayer
{
    private Dictionary<Player, int> threatLevels = new Dictionary<Player, int>();
    
    public override IEnumerator TakeTurn(Card currentCard, EventCard drawnEvent)
    {
        // Update threat assessment before turn
        UpdateThreatLevels();
        
        return base.TakeTurn(currentCard, drawnEvent);
    }
    
    void UpdateThreatLevels()
    {
        threatLevels.Clear();
        
        foreach (var player in GameManager.Instance.players)
        {
            if (player == this) continue;
            
            int threat = 0;
            
            // High EVs = high threat
            threat += player.totalElectoralVotes / 10;
            
            // Institutions = threat
            threat += player.capturedInstitutions.Count * 20;
            
            // Check if close to winning
            if (player.totalElectoralVotes >= 250) threat += 50;
            if (player.capturedInstitutions.Count >= 3) threat += 40;
            
            threatLevels[player] = threat;
        }
    }
    
    protected override Player SelectStrategicTarget(List<Player> availablePlayers)
    {
        // Target highest threat player
        Player highestThreat = null;
        int maxThreat = 0;
        
        foreach (var player in availablePlayers)
        {
            if (threatLevels.TryGetValue(player, out int threat))
            {
                if (threat > maxThreat)
                {
                    maxThreat = threat;
                    highestThreat = player;
                }
            }
        }
        
        return highestThreat ?? availablePlayers[0];
    }
    
    protected override float EvaluateEventValue(EventCard eventCard, Card targetCard)
    {
        float baseValue = base.EvaluateEventValue(eventCard, targetCard);
        
        // Adjust based on game state
        if (totalElectoralVotes >= 250)
        {
            // Close to winning - value events more
            baseValue *= 1.3f;
        }
        
        // Save powerful events for critical moments
        if (eventCard.eventType == EventType.Challenge && totalElectoralVotes < 200)
        {
            baseValue *= 0.5f; // Save for later
        }
        
        return Mathf.Clamp01(baseValue);
    }
}