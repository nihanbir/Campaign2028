using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Institution", menuName = "Campaign 2028/Institution Card")]
public class InstitutionCardSO : ScriptableObject
{
    public string institutionName;
    [TextArea(2, 4)]
    public string description;
    public Sprite artwork;
    
    public InstitutionType institutionType;
    public List<int> redSuccessRolls = new List<int>();
    public List<int> blueSuccessRolls = new List<int>();
    
    public InstitutionCard ToCard()
    {
        return new InstitutionCard
        {
            cardName = institutionName,
            artwork = artwork,
            institutionType = institutionType,
            redSuccessRolls = new List<int>(redSuccessRolls),
            blueSuccessRolls = new List<int>(blueSuccessRolls)
        };
    }
}