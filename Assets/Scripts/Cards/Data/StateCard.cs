using System;
using System.Collections.Generic;

[Serializable]
public class StateCard : Card
{
    public override CardType CardType => CardType.State;
    public int electoralVotes;
    public List<int> redSuccessRolls = new List<int>();
    public List<int> blueSuccessRolls = new List<int>();
    public ActorTeam benefitingTeam;
    public bool hasRollAgain;
    public bool hasSecession;

    public bool IsSuccessfulRoll(int roll, ActorTeam team)
    {
        List<int> successRolls = team == ActorTeam.Red ? redSuccessRolls : blueSuccessRolls;
        return successRolls.Contains(roll);
    }
}