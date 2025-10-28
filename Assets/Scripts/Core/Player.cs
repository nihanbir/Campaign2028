using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public string playerName;
    public int playerID;
    public ActorCard assignedActor;
    public EventCard heldEvent;
    
    public PlayerDisplayCard playerDisplayCard;
    private SetupPhaseGameManager _setupPhaseGameManager;

    public void SetDisplayCard(PlayerDisplayCard newPlayerDisplayCard)
    {
        playerDisplayCard = newPlayerDisplayCard;
        playerDisplayCard.SetOwnerPlayer(this);
    }
    
}