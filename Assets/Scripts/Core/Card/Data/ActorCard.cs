

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ActorCard : Card
{
    public override CardType CardType => CardType.Actor;
    public ActorTeam team;
    public ActorType actorType;
    [HideInInspector] public int evScore;
    [HideInInspector] public int instScore;
    public Dictionary<int, int> stats = new Dictionary<int, int>
    {
        {1, 0}, // Intelligence
        {2, 0}, // Wealth
        {3, 0}, // Influence
        {4, 0}, // Victimhood
        {5, 0}, // Commitment
        {6, 0}  // Weapons
    };

    public void AddEV(int EVPoints)
    {
        evScore += EVPoints;
    }

    public void AddInstitution()
    {
        instScore++;
    }
}

public enum ActorTeam
{
    Red,
    Blue
}

public enum ActorType
{
    // Red Team
    SouthernRural,
    TechOligarch,
    TradWife,
    
    // Blue Team
    ANTIFA,
    SocialJusticeKaren,
    PodcastPundit
}