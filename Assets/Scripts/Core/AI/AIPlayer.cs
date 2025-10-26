using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Base AI player class
public class AIPlayer : Player
{
    [Header("AI Settings")]
    public float decisionDelayMin = 0.5f;
    public float decisionDelayMax = 2f;
    
    [Header("AI Stats")]
    public int failedAttempts = 0;

    [Header("SetupPhase")] 
    public bool rolledDice;
    
    // Main AI turn logic
    public virtual IEnumerator RollDice()
    {
        
        yield return new WaitForSeconds(0.5f);
        
        SetupPhaseUIManager.Instance.OnRollDiceClicked();
    }
    
    public virtual IEnumerator AssignActorToAnotherPlayer()
    {
        
        yield return new WaitForSeconds(0.5f);
        
        var unassignedPlayers = new List<UnassignedPlayerDisplayCard>();
        Debug.Log("Actual unassigned players count: " + SetupPhaseUIManager.Instance.unassignedPlayerCards.Count);
            
        foreach (var playerDisplayCardCard in SetupPhaseUIManager.Instance.unassignedPlayerCards)
        {
            if (playerDisplayCardCard.owningPlayer != this)
            {
                unassignedPlayers.Add(playerDisplayCardCard);
            }
        }
            
        var actorCardIndex = Random.Range(0, SetupPhaseUIManager.Instance.unassignedActorCards.Count - 1);
        var playerIndex = Random.Range(0, unassignedPlayers.Count - 1);

        Debug.Log("Unassigned players count: " + unassignedPlayers.Count);
        Debug.Log("Selected playerIndex: " + playerIndex);
            
            
        var selectedActorCard = SetupPhaseUIManager.Instance.unassignedActorCards[actorCardIndex];
        var selectedPlayer = unassignedPlayers[playerIndex];

        SetupPhaseUIManager.Instance.SelectActorCard(selectedActorCard);
        SetupPhaseUIManager.Instance.AssignSelectedActorToPlayer(selectedPlayer.owningPlayer, selectedPlayer);
    }
    
}