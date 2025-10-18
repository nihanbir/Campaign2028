using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public string playerName;
    public int playerID;
    public ActorCard assignedActor;
    public AllegianceCard allegiance;
    
    // Victory tracking
    public int totalElectoralVotes;
    public List<StateCard> capturedStates = new List<StateCard>();
    public List<InstitutionCard> capturedInstitutions = new List<InstitutionCard>();
    
    // Event holding
    public EventCard heldEvent;
    public bool isHoldingEvent => heldEvent != null;
    
    // Battle stats
    public bool isEliminated = false;
    public int initiativeRoll;
    
    // Add captured state
    public void CaptureState(StateCard state)
    {
        capturedStates.Add(state);
        totalElectoralVotes += state.electoralVotes;
        Debug.Log($"{playerName} captured {state.cardName} (+{state.electoralVotes} EVs). Total: {totalElectoralVotes}");
        
        CheckVictoryConditions();
    }
    
    // Add captured institution
    public void CaptureInstitution(InstitutionCard institution)
    {
        capturedInstitutions.Add(institution);
        Debug.Log($"{playerName} captured {institution.cardName}. Total Institutions: {capturedInstitutions.Count}");
        
        CheckVictoryConditions();
    }
    
    // Remove state (secession)
    public void RemoveState(StateCard state)
    {
        if (capturedStates.Contains(state))
        {
            capturedStates.Remove(state);
            totalElectoralVotes -= state.electoralVotes;
            Debug.Log($"{state.cardName} seceded from {playerName}. Total EVs: {totalElectoralVotes}");
        }
    }
    
    // Hold or discard event
    public void HoldEvent(EventCard eventCard)
    {
        if (heldEvent != null)
        {
            Debug.Log($"{playerName} discarded held event: {heldEvent.cardName}");
        }
        heldEvent = eventCard;
        Debug.Log($"{playerName} is holding: {eventCard.cardName}");
    }
    
    public void UseHeldEvent()
    {
        if (heldEvent != null)
        {
            Debug.Log($"{playerName} uses held event: {heldEvent.cardName}");
            heldEvent = null;
        }
    }
    
    // Victory checks
    public bool CheckVictoryConditions()
    {
        // 290 EVs win
        if (totalElectoralVotes >= 290)
        {
            Debug.Log($"🎉 {playerName} WINS with {totalElectoralVotes} Electoral Votes!");
            return true;
        }
        
        // 4 Institutions including CIA
        if (capturedInstitutions.Count >= 4)
        {
            bool hasCIA = capturedInstitutions.Exists(inst => inst.institutionType == InstitutionType.CIA);
            if (hasCIA)
            {
                Debug.Log($"🎉 {playerName} WINS by controlling 4 Institutions including CIA!");
                return true;
            }
        }
        
        return false;
    }
    
    // Battle methods
    public void RollInitiative()
    {
        initiativeRoll = Random.Range(1, 7);
        Debug.Log($"{playerName} initiative: {initiativeRoll}");
    }
    
    public void Attack(Player target)
    {
        if (isEliminated || target.isEliminated) return;
        
        int attributeRoll = Random.Range(1, 7);
        int attackRoll = Random.Range(1, 7);
        
        int defenderStat = target.assignedActor.stats[attributeRoll];
        
        Debug.Log($"{playerName} attacks {target.playerName}! Attribute: {attributeRoll}, Attack: {attackRoll} vs Defense: {defenderStat}");
        
        if (attackRoll >= defenderStat)
        {
            target.isEliminated = true;
            Debug.Log($"💀 {target.playerName} has been eliminated!");
        }
        else
        {
            Debug.Log($"{target.playerName} survived the attack.");
        }
    }
    
    public ActorTeam GetTeam()
    {
        return assignedActor != null ? assignedActor.team : ActorTeam.Red;
    }
}