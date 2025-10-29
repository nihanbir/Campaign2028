using System.Collections;
using UnityEngine;

public class MainPhaseAIManager
{
    private readonly AIManager aiManager;

    public MainPhaseAIManager(AIManager manager)
    {
        aiManager = manager;
    }

    public IEnumerator ExecuteAITurn(AIPlayer aiPlayer, EventCard card)
    {
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));

        // Think delay or future logic (use saved events, modifiers, etc.)
        // Debug.Log($"AI Player {aiPlayer.playerID} rolling dice for Main Phase");

        yield return aiManager.StartCoroutine(HandleEventCard(aiPlayer, card));
        
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));
        yield return aiManager.StartCoroutine(RollDice(aiPlayer));
    }
    
    private IEnumerator RollDice(AIPlayer aiPlayer)
    {
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));
        GameUIManager.Instance.mainUI.OnRollDiceClicked();
    }
    
    private IEnumerator HandleEventCard(AIPlayer aiPlayer, EventCard card)
    {
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));
        
        if (card.mustPlayImmediately)
        {
            GameManager.Instance.mainPhase.ApplyEventEffect(aiPlayer, card);
        }
        else if (card.canSave)
        {
            if (!GameManager.Instance.mainPhase.TrySaveEvent(aiPlayer, card))
            {
                GameManager.Instance.mainPhase.ApplyEventEffect(aiPlayer, card);
            }
        }
    }
}