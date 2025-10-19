using UnityEngine;

[CreateAssetMenu(fileName = "New Actor", menuName = "Campaign 2028/Actor Card")]
public class ActorCardSO : ScriptableObject
{
    public string actorName;
    [TextArea(2, 4)]
    public Sprite artwork;
    
    public ActorTeam team;
    public ActorType actorType;
    
    [Header("Battle Stats")]
    public int intelligence = 3;   // Die 1
    public int wealth = 3;         // Die 2
    public int influence = 3;      // Die 3
    public int victimhood = 3;     // Die 4
    public int commitment = 3;     // Die 5
    public int weapons = 3;        // Die 6
    
    public ActorCard ToCard()
    {
        var card = new ActorCard
        {
            cardName = actorName,
            artwork = artwork,
            team = team,
            actorType = actorType
        };
        
        card.stats[1] = intelligence;
        card.stats[2] = wealth;
        card.stats[3] = influence;
        card.stats[4] = victimhood;
        card.stats[5] = commitment;
        card.stats[6] = weapons;
        
        return card;
    }
}