using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SetupPhaseAIManager
{
    private readonly AIManager aiManager;

    public SetupPhaseAIManager(AIManager manager)
    {
        aiManager = manager;
    }

    public IEnumerator ExecuteAITurn(AIPlayer aiPlayer)
    {
        // Simulate thinking
        yield return new WaitForSeconds(1f);

        var setupPhase = GameManager.Instance.setupPhase;
        if (setupPhase.CurrentStage == SetupStage.Roll ||
            setupPhase.CurrentStage == SetupStage.Reroll)
        {
            yield return aiManager.StartCoroutine(RollDice(aiPlayer));
        }
        else
        {
            yield return aiManager.StartCoroutine(AssignActorToAnotherPlayer(aiPlayer));
        }
    }

    private IEnumerator RollDice(AIPlayer aiPlayer)
    {
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));
        GameUIManager.Instance.setupUI.OnRollDiceClicked();
    }

    private IEnumerator AssignActorToAnotherPlayer(AIPlayer aiPlayer)
    {
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));
        
        // Get all unassigned players except this AI
        var eligiblePlayers = GameUIManager.Instance.setupUI.unassignedPlayerCards
            .Where(card => card.owningPlayer != aiPlayer)
            .ToList();
            
        if (eligiblePlayers.Count == 0)
        {
            Debug.LogWarning($"AI Player {aiPlayer.playerID}: No eligible players to assign actor to!");
            yield break;
        }
        
        var availableActors = GameUIManager.Instance.setupUI.unassignedActorCards;
        if (availableActors.Count == 0)
        {
            Debug.LogWarning($"AI Player {aiPlayer.playerID}: No available actors to assign!");
            yield break;
        }
        
        int actorIndex = Random.Range(0, availableActors.Count);
        int playerIndex = Random.Range(0, eligiblePlayers.Count);

        var selectedActor = availableActors[actorIndex];
        var selectedPlayer = eligiblePlayers[playerIndex];

        Debug.Log($"AI Player {aiPlayer.playerID} assigning {selectedActor.GetCard().cardName} to Player {selectedPlayer.owningPlayer.playerID}");

        //TODO: Clean all this
        GameUIManager.Instance.setupUI.SelectActorCard(selectedActor);
        GameUIManager.Instance.setupUI.AssignSelectedActorToPlayer(selectedPlayer.owningPlayer, selectedPlayer);
    }
    
}