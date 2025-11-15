using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class Player : MonoBehaviour
{
    // === Identity ===
    public string PlayerName { get; private set; }

    public int playerID;
    public ActorCard assignedActor;

    // === Cards ===
    private readonly List<StateCard> _heldStates = new();
    private readonly List<InstitutionCard> _heldInstitutions = new();
    public EventCard HeldEvent { get; private set; }

    // === Display ===
    public PlayerDisplayCard PlayerDisplayCard { get; private set; }

    // === Roll System ===
    private int _remainingRolls = 1;

    private void Start()
    {
        HeldEvent = null;
    }

    public void SetDisplayCard(PlayerDisplayCard displayCard)
    {
        PlayerDisplayCard = displayCard;
        PlayerDisplayCard.SetOwnerPlayer(this);
    }

    // === Card Management ===
    public void CaptureCard(Card card)
    {
        if (card == null) return;

        card.isCaptured = true;

        switch (card)
        {
            case StateCard stateCard:
                _heldStates.Add(stateCard);
                assignedActor.AddEV(stateCard.electoralVotes);
                break;

            case InstitutionCard institutionCard:
                _heldInstitutions.Add(institutionCard);
                assignedActor.AddInstitution();
                break;
        }
        
        //TODO: tie these up nicer
        PlayerDisplayCard.UpdateScore();
    }

    //TODO: notify ui to update player info i guess
    public void RemoveCapturedCard(Card card)
    {
        if (card == null) return;

        card.isCaptured = false;

        switch (card)
        {
            case StateCard stateCard:
                _heldStates.Remove(stateCard);
                assignedActor.AddEV(-stateCard.electoralVotes);
                break;

            case InstitutionCard institutionCard:
                _heldInstitutions.Remove(institutionCard);
                assignedActor.RemoveInstitution();
                break;
        }
        
        Debug.Log($"Player {playerID} lost {card.cardName}");
        PlayerDisplayCard.UpdateScore();
    }

    public void SaveEvent(EventCard eventCard)
    {
        HeldEvent = eventCard;
    }

    public bool HasInstitution(InstitutionCard target)
    {
        return _heldInstitutions.Contains(target);
    }

    // === Roll System ===
    public void AddExtraRoll()
    {
        _remainingRolls++;
        Debug.Log($"Player {playerID} gained an extra roll!");
    }

    public bool CanRoll() => _remainingRolls > 0;

    public void RegisterRoll() => _remainingRolls = Mathf.Max(0, _remainingRolls - 1);

    public void ResetRollCount() => _remainingRolls = 1;
}
