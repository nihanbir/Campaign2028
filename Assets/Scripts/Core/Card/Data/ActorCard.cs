

using System;
using System.Collections.Generic;

[Serializable]
public class ActorCard : Card
{
    public ActorTeam team;
    public ActorType actorType;
    public Dictionary<int, int> stats = new Dictionary<int, int>
    {
        {1, 0}, // Intelligence
        {2, 0}, // Wealth
        {3, 0}, // Influence
        {4, 0}, // Victimhood
        {5, 0}, // Commitment
        {6, 0}  // Weapons
    };
}

public enum ActorTeam
{
    Red,
    Blue
}

public enum ActorType
{
    SouthernRural,
    TechOligarch,
    TradWife,
    ANTIFA,
    SocialJusticeKaren,
    PodcastPundit
}