using System.Collections;
using UnityEngine;

public class MainPhaseAIManager
{
    private readonly AIManager aiManager;

    public MainPhaseAIManager(AIManager manager)
    {
        aiManager = manager;
    }

    public IEnumerator ExecuteAITurn(AIPlayer aiPlayer, MainPhaseGameManager phase)
    {
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));

        // Think delay or future logic (use saved events, modifiers, etc.)
        Debug.Log($"AI Player {aiPlayer.playerID} rolling dice for Main Phase");

        yield return aiManager.StartCoroutine(RollDice(aiPlayer));
    }
    
    public virtual IEnumerator RollDice(AIPlayer aiPlayer)
    {
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));
        GameUIManager.Instance.mainUI.OnRollDiceClicked();
    }
}