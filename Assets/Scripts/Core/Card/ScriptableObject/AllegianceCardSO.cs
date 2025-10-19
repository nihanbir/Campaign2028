using UnityEngine;

[CreateAssetMenu(fileName = "New Allegiance", menuName = "Campaign 2028/Allegiance Card")]
public class AllegianceCardSO : ScriptableObject
{
    public string allegianceName;
    [TextArea(2, 4)]
    public Sprite artwork;
    
    public AllegianceType allegiance;
    
    public AllegianceCard ToCard()
    {
        return new AllegianceCard
        {
            cardName = allegianceName,
            artwork = artwork,
            allegiance = allegiance
        };
    }
}