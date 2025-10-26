using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

// Manages all AI players in the game
public class SetupPhaseAIManager : MonoBehaviour
{
    public static SetupPhaseAIManager Instance;
    
    
    [Header("AI Configuration")]
    public int numberOfAIPlayers = 5;
    public bool mixPersonalities = true;
    public GameObject aiPlayerPrefab;
    
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
        ai.playerID = 100 + index; // AI IDs start at 100
        
        aiPlayers.Add(ai);
        
        // Register with game manager
        if (SetupPhaseGameManager.Instance != null)
        {
            SetupPhaseGameManager.Instance.players.Add(ai);
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
    
    public IEnumerator ExecuteAITurn(AIPlayer aiPlayer)
    {
        // Wait a short delay to simulate thinking
        yield return new WaitForSeconds(1f);
        
        if (SetupPhaseGameManager.Instance.CurrentStage == SetupStage.Roll 
                            || SetupPhaseGameManager.Instance.CurrentStage == SetupStage.Reroll)
        {
            yield return StartCoroutine(aiPlayer.RollDice());
        }
        else
        {
            yield return StartCoroutine(aiPlayer.AssignActorToAnotherPlayer());
        }
    }
}