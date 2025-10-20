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
    public int turnsPlayed = 0;
    public int successfulCaptures = 0;
    public int failedAttempts = 0;
    
    // AI decision weights
    protected float aggressiveness = 0.5f;  // 0-1: How likely to take risks
    protected float patience = 0.5f;        // 0-1: How likely to save events
    protected float strategic = 0.5f;       // 0-1: How much to plan ahead
    
    
    // Main AI turn logic
    public virtual IEnumerator TakeTurn(Card currentCard, EventCard drawnEvent)
    {
        turnsPlayed++;
        
        yield return new WaitForSeconds(Random.Range(decisionDelayMin, decisionDelayMax));
        
        // Step 1: Decide on event usage
        bool useEvent = DecideEventUsage(drawnEvent, currentCard);
        
        if (useEvent && drawnEvent != null)
        {
            yield return StartCoroutine(UseEvent(drawnEvent));
        }
        
        // Step 2: Decide whether to use held event
        if (heldEvent != null)
        {
            bool useHeld = DecideUseHeldEvent(currentCard);
            if (useHeld)
            {
                yield return StartCoroutine(UseHeldEvent());
            }
        }
        
        // Step 3: Roll the dice
        yield return new WaitForSeconds(0.5f);
        Debug.Log($"{playerName} (AI) is rolling...");
        
        // Notify game manager to process roll
        GameManager.Instance.RollDie();
        
    }
    
    // Decide whether to use the drawn event
    protected virtual bool DecideEventUsage(EventCard eventCard, Card targetCard)
    {
        if (eventCard == null) return false;
        
        // Must play immediately
        if (eventCard.mustPlayImmediately) return true;
        
        // Can't save if already holding one
        if (IsHoldingEvent && eventCard.canSave) return false;
        
        // Evaluate event value
        float eventValue = EvaluateEventValue(eventCard, targetCard);
        
        // Decision based on personality
        if (eventValue > 0.7f) return true; // Very valuable - use it
        if (eventValue < 0.3f && eventCard.canSave) 
        {
            // Save for later if patient
            return Random.value > patience;
        }
        
        return Random.value > 0.5f;
    }
    
    // Evaluate how valuable an event is right now
    protected virtual float EvaluateEventValue(EventCard eventCard, Card targetCard)
    {
        float value = 0.5f; // Base value
        
        if (targetCard == null) return 0f;
        
        switch (eventCard.eventType)
        {
            case EventType.ExtraRoll:
                // More valuable for high-EV states
                if (targetCard is StateCard state)
                {
                    value = state.electoralVotes / 54f; // Normalized by max (California)
                    
                    // Check our odds
                    float successChance = CalculateSuccessChance(state);
                    if (successChance < 0.5f) value += 0.3f; // Extra roll helps bad odds
                }
                break;
                
            case EventType.NeedTwo:
                // Generally low value unless we have good odds anyway
                value = 0.2f;
                break;
                
            case EventType.Challenge:
                // Valuable if we're behind
                value = EvaluateChallenge();
                break;
                
            case EventType.ChineseAgent:
                // High value for China allegiance
                if (allegiance != null && allegiance.allegiance == AllegianceType.China)
                {
                    value = 1f;
                }
                break;
        }
        
        return Mathf.Clamp01(value);
    }
    
    // Calculate our success chance on current card
    protected float CalculateSuccessChance(StateCard state)
    {
        if (state == null || assignedActor == null) return 0.33f;
        
        List<int> successRolls = assignedActor.team == ActorTeam.Red ? 
            state.redSuccessRolls : state.blueSuccessRolls;
            
        return successRolls.Count / 6f;
    }
    
    protected float CalculateSuccessChance(InstitutionCard institution)
    {
        if (institution == null || assignedActor == null) return 0.33f;
        
        List<int> successRolls = assignedActor.team == ActorTeam.Red ? 
            institution.redSuccessRolls : institution.blueSuccessRolls;
            
        return successRolls.Count / 6f;
    }
    
    // Decide whether to use held event
    protected virtual bool DecideUseHeldEvent(Card targetCard)
    {
        if (heldEvent == null || targetCard == null) return false;
        
        float eventValue = EvaluateEventValue(heldEvent, targetCard);
        
        // Strategic AIs wait for better opportunities
        if (strategic > 0.7f)
        {
            // Only use if high value or running out of time
            if (eventValue > 0.8f) return true;
            if (totalElectoralVotes > 200) return eventValue > 0.5f; // Late game - more willing
            return false;
        }
        
        // Aggressive AIs use events quickly
        if (aggressiveness > 0.7f)
        {
            return eventValue > 0.3f;
        }
        
        return eventValue > 0.6f;
    }
    
    // Evaluate challenge event value
    protected float EvaluateChallenge()
    {
        if (GameManager.Instance == null) return 0.3f;
        
        // Find player with most institutions
        Player topPlayer = null;
        int maxInstitutions = 0;
        
        foreach (var player in GameManager.Instance.players)
        {
            if (player == this) continue;
            if (player.capturedInstitutions.Count > maxInstitutions)
            {
                maxInstitutions = player.capturedInstitutions.Count;
                topPlayer = player;
            }
        }
        
        // If someone is close to winning via institutions, challenge is valuable
        if (maxInstitutions >= 3) return 0.9f;
        
        // If we're behind, challenge is valuable
        if (totalElectoralVotes < GetAverageEVs() - 50) return 0.7f;
        
        return 0.4f;
    }
    
    protected int GetAverageEVs()
    {
        if (GameManager.Instance == null) return 0;
        
        int total = 0;
        foreach (var player in GameManager.Instance.players)
        {
            total += player.totalElectoralVotes;
        }
        return total / GameManager.Instance.players.Count;
    }
    
    // Execute event
    protected virtual IEnumerator UseEvent(EventCard eventCard)
    {
        Debug.Log($"{playerName} (AI) uses event: {eventCard.cardName}");
        yield return new WaitForSeconds(0.3f);
        
        // Event-specific logic will be handled by GameManager
    }
    
    protected virtual IEnumerator UseHeldEvent()
    {
        Debug.Log($"{playerName} (AI) uses held event: {heldEvent.cardName}");
        UseHeldEvent();
        yield return new WaitForSeconds(0.3f);
    }
    
    // Civil War AI decision making
    public virtual Player SelectAttackTarget(List<Player> availablePlayers)
    {
        if (availablePlayers == null || availablePlayers.Count == 0) return null;
        
        Player target = null;
        
        Debug.Log($"{playerName} (AI) targets {target.playerName} for attack");
        return target;
    }
    
    protected virtual Player SelectStrategicTarget(List<Player> availablePlayers)
    {
        // Priority 1: Target player closest to winning
        Player closestToWin = availablePlayers.OrderByDescending(p => p.totalElectoralVotes).First();
        if (closestToWin.totalElectoralVotes >= 250) return closestToWin;
        
        // Priority 2: Target player with most institutions
        Player mostInstitutions = availablePlayers.OrderByDescending(p => p.capturedInstitutions.Count).First();
        if (mostInstitutions.capturedInstitutions.Count >= 3) return mostInstitutions;
        
        // Priority 3: Target weakest player (finish them off)
        Player weakest = availablePlayers.OrderBy(p => GetPlayerStrength(p)).First();
        
        // 70% chance to target strategic, 30% to eliminate weak
        return Random.value < 0.7f ? closestToWin : weakest;
    }
    
    protected int GetPlayerStrength(Player player)
    {
        // Combine EVs and institutions for overall strength score
        return player.totalElectoralVotes + (player.capturedInstitutions.Count * 30);
    }
    
    // Coalition decision making
    public virtual bool DecideJoinCoalition(Player inviter)
    {
        if (inviter == null) return false;
        
        // Calculate if coalition benefits us
        int ourStrength = GetPlayerStrength(this);
        int theirStrength = GetPlayerStrength(inviter);
        int combinedStrength = ourStrength + theirStrength;
        
        // Find strongest opponent
        int maxOpponentStrength = 0;
        foreach (var player in GameManager.Instance.players)
        {
            if (player == this || player == inviter) continue;
            int strength = GetPlayerStrength(player);
            if (strength > maxOpponentStrength) maxOpponentStrength = strength;
        }
        
        // Join if combined strength would beat strongest opponent
        bool shouldJoin = combinedStrength > maxOpponentStrength * 1.2f;
        
        Debug.Log($"{playerName} (AI) {(shouldJoin ? "accepts" : "declines")} coalition with {inviter.playerName}");
        return shouldJoin;
    }
    
}