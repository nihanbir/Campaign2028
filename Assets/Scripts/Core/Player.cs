using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Player : MonoBehaviour
{
    public string playerName;
    public int playerID;
    public ActorCard assignedActor;
    
    private List<StateCard> _heldStates = new ();
    private List<InstitutionCard> _heldInstitutions = new ();

    public EventCard heldEvent;
    
    public PlayerDisplayCard playerDisplayCard;
    private SetupPhaseGameManager _setupPhaseGameManager;

    private int _rollCount = 1;

    public void SetDisplayCard(PlayerDisplayCard newPlayerDisplayCard)
    {
        playerDisplayCard = newPlayerDisplayCard;
        playerDisplayCard.SetOwnerPlayer(this);
    }

    public void CaptureCard(Card card)
    {
        card.isCaptured = true;
        
        switch (card)
        {
            case StateCard stateCard:
                _heldStates.Add(stateCard);
                assignedActor.evScore += stateCard.electoralVotes;
                Debug.Log($"Player {playerID} gained {stateCard.electoralVotes} EV from {stateCard.cardName}");
                Debug.Log($"Player {playerID} new EV score: {assignedActor.evScore}");
                break;

            case InstitutionCard institutionCard:
                _heldInstitutions.Add(institutionCard);
                assignedActor.instScore++;
                Debug.Log($"Player {playerID} captured an Institution: {institutionCard.cardName}");
                Debug.Log($"Player {playerID} new Inst score: {assignedActor.instScore}");
                break;
        }
    }

    public bool HasInstitution(InstitutionCard institution)
    {
        return _heldInstitutions.Any(inst => inst == institution);
    }

    public void AddExtraRoll()
    {
        Debug.Log("added extra roll");
        _rollCount++;
    }

    public bool CanRoll()
    {
        return _rollCount != 0;
    }

    public void ResetPlayerRollCount()
    {
        _rollCount = 1;
    }
    
}