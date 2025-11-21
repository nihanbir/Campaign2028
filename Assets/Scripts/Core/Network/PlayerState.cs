using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Network-safe player state representation.
/// Contains only data, no logic. Can be serialized for network transmission.
/// </summary>
[Serializable]
public class PlayerState
{
    public int PlayerId;
    public string PlayerName;
    public string AssignedActorCardId;
    
    // Cards held by this player
    public List<string> HeldStateCardIds = new();
    public List<string> HeldInstitutionCardIds = new();
    public string HeldEventCardId;
    
    // Roll state
    public int RemainingRolls = 1;
    public int LastRoll = 0;
    
    // Computed scores (cached from actor/cards)
    public int ElectoralVotes = 0;
    public int InstitutionCount = 0;
    
    // Flags
    public bool IsAI = false;
    public bool HasRolled = false;
    
    public bool CanRoll => RemainingRolls > 0;
    
    public PlayerState(int playerId, bool isAI = false)
    {
        PlayerId = playerId;
        IsAI = isAI;
        PlayerName = $"Player {playerId}";
    }
    
    public void ResetRolls()
    {
        RemainingRolls = 1;
        HasRolled = false;
    }
    
    public void AddExtraRoll()
    {
        RemainingRolls++;
    }
    
    public void ConsumeRoll()
    {
        RemainingRolls = Mathf.Max(0, RemainingRolls - 1);
        HasRolled = true;
    }
    
    public void AddStateCard(string cardId, int electoralVotes)
    {
        if (!HeldStateCardIds.Contains(cardId))
        {
            HeldStateCardIds.Add(cardId);
            ElectoralVotes += electoralVotes;
        }
    }
    
    public void RemoveStateCard(string cardId, int electoralVotes)
    {
        if (HeldStateCardIds.Remove(cardId))
        {
            ElectoralVotes -= electoralVotes;
        }
    }
    
    public void AddInstitutionCard(string cardId)
    {
        if (!HeldInstitutionCardIds.Contains(cardId))
        {
            HeldInstitutionCardIds.Add(cardId);
            InstitutionCount++;
        }
    }
    
    public void RemoveInstitutionCard(string cardId)
    {
        if (HeldInstitutionCardIds.Remove(cardId))
        {
            InstitutionCount--;
        }
    }
    
    public bool HasInstitution(string institutionCardId)
    {
        return HeldInstitutionCardIds.Contains(institutionCardId);
    }
    
    /// <summary>
    /// Serialize for network transmission
    /// </summary>
    public byte[] Serialize()
    {
        // Implement based on your networking solution
        // Example: use JsonUtility, BinaryFormatter, or custom serialization
        return System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(this));
    }
    
    /// <summary>
    /// Deserialize from network data
    /// </summary>
    public static PlayerState Deserialize(byte[] data)
    {
        string json = System.Text.Encoding.UTF8.GetString(data);
        return JsonUtility.FromJson<PlayerState>(json);
    }
    
    public PlayerState Clone()
    {
        var clone = new PlayerState(PlayerId, IsAI)
        {
            PlayerName = PlayerName,
            AssignedActorCardId = AssignedActorCardId,
            HeldStateCardIds = new List<string>(HeldStateCardIds),
            HeldInstitutionCardIds = new List<string>(HeldInstitutionCardIds),
            HeldEventCardId = HeldEventCardId,
            RemainingRolls = RemainingRolls,
            LastRoll = LastRoll,
            ElectoralVotes = ElectoralVotes,
            InstitutionCount = InstitutionCount,
            HasRolled = HasRolled
        };
        return clone;
    }
}