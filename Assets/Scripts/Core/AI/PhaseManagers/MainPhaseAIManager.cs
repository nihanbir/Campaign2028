using System.Collections;
using UnityEngine;

public class MainPhaseAIManager
{
    private readonly AIManager aiManager;
    private MainPhaseGameManager _mainPhase;
    private EventManager _eventManager;

    private AIPlayer _currentAIPlayer;
    

    public MainPhaseAIManager(AIManager manager)
    {
        aiManager = manager;
    }

    public void InitializeAIManager()
    {
        _mainPhase = GameManager.Instance.mainPhase;
        _eventManager = _mainPhase.EventManager;

    }

    public IEnumerator ExecuteAITurn(AIPlayer aiPlayer, EventCard card)
    {
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));

        //TODO: Think delay or future logic (use saved events, modifiers, etc.)

        if (card != null)
        {
            yield return aiManager.StartCoroutine(HandleEventCard(aiPlayer, card));
        }
        
        if (GameManager.Instance.CurrentPlayer == aiPlayer)
        {
            yield return aiManager.StartCoroutine(RollDice(aiPlayer));
        }
        else
        {
            Debug.Log("AI lost its turn");
        }
    }
    
    private IEnumerator RollDice(AIPlayer aiPlayer)
    {
        Debug.Log("Ai is rolling");
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));
        GameUIManager.Instance.mainUI.OnRollDiceClicked();
    }
    
    private IEnumerator HandleEventCard(AIPlayer aiPlayer, EventCard card)
    {
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));
        
        Debug.Log("HANDLING EVENT CARD");
        bool resolved = false;
        void OnApplied(EventCard appliedCard)
        {
            if (appliedCard == card)
                resolved = true;
        }

        _eventManager.OnEventApplied += OnApplied; // 🔹 subscribe BEFORE ApplyEvent
        
        if (card.mustPlayImmediately)
        {
            _mainPhase.EventManager.ApplyEvent(aiPlayer, card);
        }
        else if (card.canSave)
        {
            if (!_mainPhase.TrySaveEvent(card))
            {
                _mainPhase.EventManager.ApplyEvent(aiPlayer, card);
            }
            else
            {
                resolved = true;
            }
        }
        
        // Wait until UI and game logic both finish
        yield return new WaitUntil(() => resolved);

        _eventManager.OnEventApplied -= OnApplied;
    }
}