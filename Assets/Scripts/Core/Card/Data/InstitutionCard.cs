using System;
using System.Collections.Generic;

[Serializable]
public class InstitutionCard : Card
{
    public InstitutionType institutionType;
    public List<int> redSuccessRolls = new List<int>();
    public List<int> blueSuccessRolls = new List<int>();

    public bool IsSuccessfulRoll(int roll, ActorTeam team)
    {
        List<int> successRolls = team == ActorTeam.Red ? redSuccessRolls : blueSuccessRolls;
        return successRolls.Contains(roll);
    }
}
public enum InstitutionType
{
    SupremeCourt,
    Congress,
    Media,
    WhiteHouse,
    Pentagon,
    CIA,
    WallStreet
}