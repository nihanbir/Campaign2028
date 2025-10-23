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
    
}