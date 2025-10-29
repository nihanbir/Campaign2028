using System;
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

    public void UpdatePlayerScore(Card card)
    {
        // Different logic depending on what kind of card was captured
        switch (card)
        {
            case StateCard stateCard:
                assignedActor.evScore += stateCard.electoralVotes;
                Debug.Log($"Player {playerID} gained {stateCard.electoralVotes} EV from {stateCard.cardName}");
                Debug.Log($"Player {playerID} new EV score: {assignedActor.evScore}");
                
                break;

            case InstitutionCard:
                assignedActor.instScore++;
                Debug.Log($"Player {playerID} captured an Institution: {card.cardName}");
                Debug.Log($"Player {playerID} new Inst score: {assignedActor.instScore}");
                
                break;
        }

    }
    
}