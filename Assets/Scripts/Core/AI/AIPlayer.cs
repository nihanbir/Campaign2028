using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AIPlayer : Player
{
    [Header("AI Settings")]
    public float decisionDelayMin = 0.5f;
    public float decisionDelayMax = 2f;
    
    [Header("AI Stats")]
    public int failedAttempts = 0;

    public virtual IEnumerator RollDice()
    {
        yield return new WaitForSeconds(Random.Range(decisionDelayMin, decisionDelayMax));
        GameUIManager.Instance.setupUI.OnRollDiceClicked();
    }
    
    public virtual IEnumerator AssignActorToAnotherPlayer()
    {
        yield return new WaitForSeconds(Random.Range(decisionDelayMin, decisionDelayMax));
        
        // Get all unassigned players except this AI
        var eligiblePlayers = GameUIManager.Instance.setupUI.unassignedPlayerCards
            .Where(card => card.owningPlayer != this)
            .ToList();
            
        if (eligiblePlayers.Count == 0)
        {
            Debug.LogWarning($"AI Player {playerID}: No eligible players to assign actor to!");
            failedAttempts++;
            yield break;
        }
        
        var availableActors = GameUIManager.Instance.setupUI.unassignedActorCards;
        if (availableActors.Count == 0)
        {
            Debug.LogWarning($"AI Player {playerID}: No available actors to assign!");
            failedAttempts++;
            yield break;
        }

        // BUGFIX: Random.Range with integers is exclusive of max value
        // So Random.Range(0, list.Count - 1) could never select the last item
        int actorIndex = Random.Range(0, availableActors.Count);
        int playerIndex = Random.Range(0, eligiblePlayers.Count);

        var selectedActor = availableActors[actorIndex];
        var selectedPlayer = eligiblePlayers[playerIndex];

        Debug.Log($"AI Player {playerID} assigning {selectedActor.GetActorCard().cardName} to Player {selectedPlayer.owningPlayer.playerID}");

        GameUIManager.Instance.setupUI.SelectActorCard(selectedActor);
        GameUIManager.Instance.setupUI.AssignSelectedActorToPlayer(selectedPlayer.owningPlayer, selectedPlayer);
    }
    
    // Optional: More advanced AI decision making
    protected virtual ActorCard ChooseActorStrategically(List<PlayerDisplayCard> availableActors)
    {
        // Base implementation: random choice
        // Override this in derived classes for smarter AI
        int index = Random.Range(0, availableActors.Count);
        return availableActors[index].GetActorCard();
    }
    
    protected virtual Player ChoosePlayerStrategically(List<PlayerDisplayCard> eligiblePlayers, ActorCard actorToAssign)
    {
        // Base implementation: random choice
        // Override this in derived classes for smarter AI (e.g., give weak actors to strong opponents)
        int index = Random.Range(0, eligiblePlayers.Count);
        return eligiblePlayers[index].owningPlayer;
    }
}