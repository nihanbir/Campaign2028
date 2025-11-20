using UnityEngine;

[CreateAssetMenu(fileName = "New Actor", menuName = "Campaign 2028/Actor Card")]
public class ActorCardSO : ScriptableObject
{
    public string actorName;
    public Sprite artwork;
    
    public ActorTeam team;
    public ActorType actorType;
    
    [HideInInspector] public int evScore;
    [HideInInspector] public int instScore;
    
    [Header("Battle Stats")]
    public int intelligence;   // Die 1
    public int wealth;         // Die 2
    public int influence;      // Die 3
    public int victimhood;     // Die 4
    public int commitment;     // Die 5
    public int weapons;        // Die 6
    
    public ActorCard ToCard()
    {
        var card = new ActorCard
        {
            cardName = actorName,
            artwork = artwork,
            team = team,
            actorType = actorType,
            evScore = evScore,
            instScore = instScore,
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