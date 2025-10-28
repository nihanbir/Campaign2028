using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetupPhaseAIManager
{
    private readonly AIManager aiManager;
    private readonly List<AIPlayer> aiPlayers;

    public SetupPhaseAIManager(AIManager manager)
    {
        aiManager = manager;
        aiPlayers = aiManager.aiPlayers;
    }

    public IEnumerator ExecuteAITurn(AIPlayer aiPlayer)
    {
        // Simulate thinking
        yield return new WaitForSeconds(1f);

        var setupPhase = GameManager.Instance.setupPhase;
        if (setupPhase.CurrentStage == SetupStage.Roll ||
            setupPhase.CurrentStage == SetupStage.Reroll)
        {
            yield return aiManager.StartCoroutine(aiPlayer.RollDice());
        }
        else
        {
            yield return aiManager.StartCoroutine(aiPlayer.AssignActorToAnotherPlayer());
        }
    }
    
}