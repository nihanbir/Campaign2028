using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New State", menuName = "Campaign 2028/State Card")]
public class StateCardSO : ScriptableObject
{
    public string stateName;
    public Sprite artwork;
    public Sprite backSide;
    
    public int electoralVotes;
    public List<int> redSuccessRolls = new List<int>();
    public List<int> blueSuccessRolls = new List<int>();
    public bool hasRollAgain;
    public bool hasSecession;
    
    [ReadOnly] public ActorTeam benefitingTeam;
    
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
            hasSecession = hasSecession,
            benefitingTeam = benefitingTeam,
            
        };
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        benefitingTeam = GetBenefitingTeam();
        UnityEditor.EditorUtility.SetDirty(this);
    }
    
    private ActorTeam GetBenefitingTeam()
    {
        if (redSuccessRolls.Count == 0 || blueSuccessRolls.Count == 0) return ActorTeam.None;
        if (redSuccessRolls.Count > blueSuccessRolls.Count)
        {
            return ActorTeam.Red;
        }
        
        return ActorTeam.Blue;
    }
    
#endif
}