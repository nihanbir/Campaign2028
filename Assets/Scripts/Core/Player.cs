using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player - pure data class, no MonoBehaviour needed.
/// </summary>
[Serializable]
public class Player
{
    public int playerID;
    public string PlayerName;
    
    // Card data
    public ActorCard assignedActor;
    public AllegianceCard assignedAllegiance;
    public List<StateCard> HeldStateCards = new();
    public List<InstitutionCard> HeldInstitutionCards = new();
    public EventCard HeldEvent;
    
    // Roll state
    public int RemainingRolls = 1;
    public int LastRoll = 0;
    
    // Computed scores
    public int ElectoralVotes = 0;
    public int InstitutionCount = 0;
    
    // Flags
    public bool IsAI = false;
    
    // Display reference (not serialized)
    [NonSerialized] public PlayerDisplayCard PlayerDisplayCard;
    
    public bool CanRoll() => RemainingRolls > 0;
    
    public Player(int id, bool isAI = false)
    {
        playerID = id;
        IsAI = isAI;
        PlayerName = $"Player {id}";
    }
    
    public void SetDisplayCard(PlayerDisplayCard displayCard)
    {
        PlayerDisplayCard = displayCard;
        PlayerDisplayCard.SetOwnerPlayer(this);
    }
    
    // === Roll Management ===
    public void ResetRollCount()
    {
        RemainingRolls = 1;
    }
    
    public void AddExtraRoll()
    {
        RemainingRolls++;
    }
    
    public void RegisterRoll()
    {
        RemainingRolls = Mathf.Max(0, RemainingRolls - 1);
    }
    
    // === Card Management ===
    public void CaptureCard(Card card)
    {
        if (card == null) return;
        
        card.isCaptured = true;

        switch (card)
        {
            case StateCard stateCard:
                if (!HeldStateCards.Contains(stateCard))
                {
                    HeldStateCards.Add(stateCard);
                    ElectoralVotes += stateCard.electoralVotes;
                }
                break;

            case InstitutionCard institutionCard:
                if (!HeldInstitutionCards.Contains(institutionCard))
                {
                    HeldInstitutionCards.Add(institutionCard);
                    InstitutionCount++;
                }
                break;
        }
        
        PlayerDisplayCard?.UpdateScore();
    }
    
    public void RemoveCapturedCard(Card card)
    {
        if (card == null) return;

        card.isCaptured = false;

        switch (card)
        {
            case StateCard stateCard:
                if (HeldStateCards.Remove(stateCard))
                {
                    ElectoralVotes -= stateCard.electoralVotes;
                }
                break;

            case InstitutionCard institutionCard:
                if (HeldInstitutionCards.Remove(institutionCard))
                {
                    InstitutionCount--;
                }
                break;
        }
        
        Debug.Log($"Player {playerID} lost {card.cardName}");
        PlayerDisplayCard?.UpdateScore();
    }

    public void SaveEvent(EventCard eventCard)
    {
        HeldEvent = eventCard;
    }

    public bool HasInstitution(InstitutionCard target)
    {
        return HeldInstitutionCards.Contains(target);
    }
}