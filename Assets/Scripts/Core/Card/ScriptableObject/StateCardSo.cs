using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New State", menuName = "Campaign 2028/State Card")]
public class StateCardSO : ScriptableObject
{
    public string stateName;
    [TextArea(2, 4)]
    public string description;
    public Sprite artwork;
    
    public int electoralVotes;
    public List<int> redSuccessRolls = new List<int>();
    public List<int> blueSuccessRolls = new List<int>();
    public bool hasRollAgain;
    public bool hasSecession;
    
    public StateCard ToCard()
    {
        return new StateCard
        {
            cardName = stateName,
            artwork = artwork,
            electoralVotes = electoralVotes,
            redSuccessRolls = new List<int>(redSuccessRolls),
            blueSuccessRolls = new List<int>(blueSuccessRolls),
            hasRollAgain = hasRollAgain,
            hasSecession = hasSecession
        };
    }
}