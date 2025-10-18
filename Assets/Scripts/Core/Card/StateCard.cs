using System;
using System.Collections.Generic;

[Serializable]
public class StateCard : Card
{
    public int electoralVotes;
    public List<int> redSuccessRolls = new List<int>();
    public List<int> blueSuccessRolls = new List<int>();
    public bool hasRollAgain;
    public bool hasSecession;

    public bool IsSuccessfulRoll(int roll, ActorTeam team)
    {
        List<int> successRolls = team == ActorTeam.Red ? redSuccessRolls : blueSuccessRolls;
        return successRolls.Contains(roll);
    }
}