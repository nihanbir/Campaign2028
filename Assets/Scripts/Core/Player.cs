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
    public bool hasCIA = false;
    
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
                CaptureStateCard(stateCard);
                break;

            case InstitutionCard institutionCard:
                CaptureInstCard(institutionCard);
                break;
        }
        
        //TODO:move this to UI
        PlayerDisplayCard?.UpdateScore();
    }

    public void CaptureStateCard(StateCard stateCard)
    {
        if (HeldStateCards.Contains(stateCard)) return;
        HeldStateCards.Add(stateCard);
        ElectoralVotes += stateCard.electoralVotes;
    }

    public void CaptureInstCard(InstitutionCard instCard)
    {
        if (HeldInstitutionCards.Contains(instCard)) return;
        HeldInstitutionCards.Add(instCard);
        if (instCard.cardName == "CIA")
            hasCIA = true;
        InstitutionCount++;
        CheckInstWinConditions();
    }

    private void CheckStateWinConditions()
    {
        if (ElectoralVotes != 290) 
            return;
        
        TurnFlowBus.Instance.Raise(new PhaseChangeEvent(GamePhase.GameOver, new GameOverData(this, VictoryType.ElectoralVotes)));
        Debug.Log($"Player {playerID} won!");
    }
    
    private void CheckInstWinConditions()
    {
        if (!hasCIA) return;
        if (InstitutionCount != 4) 
            return;
        
        TurnFlowBus.Instance.Raise(new PhaseChangeEvent(GamePhase.GameOver, new GameOverData(this, VictoryType.Institutions)));
        Debug.Log($"Player {playerID} won!");
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