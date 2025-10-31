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

        //TODO: Think delay or future logic (use saved events, modifiers, etc.)
        //TODO: Have logic to make the ai roll after applying event card, if they didn't lose their turn
        
        yield return aiManager.StartCoroutine(HandleEventCard(aiPlayer, card));
        
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
            GameManager.Instance.mainPhase.EventManager.ApplyEvent(card, aiPlayer);
        }
        else if (card.canSave)
        {
            if (!GameManager.Instance.mainPhase.TrySaveEvent(card))
            {
                GameManager.Instance.mainPhase.EventManager.ApplyEvent(card, aiPlayer);
            }
        }
        
    }
}