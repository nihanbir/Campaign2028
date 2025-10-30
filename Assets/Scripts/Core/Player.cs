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

    // === Setup ===
    // public void Initialize(string name, int id, ActorCard actor)
    // {
    //     PlayerName = name;
    //     playerID = id;
    //     assignedActor = actor;
    // }

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

        Debug.Log($"Player {playerID} captured {card.cardName}");
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
