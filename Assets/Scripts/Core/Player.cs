using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public string playerName;
    public int playerID;
    public ActorCard assignedActor;
    
    public DisplayCard displayCard;
    private SetupPhaseGameManager _setupPhaseGameManager;

    public void SetDisplayCard(DisplayCard newDisplayCard)
    {
        displayCard = newDisplayCard;
    }
    
}