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
        
        // if (GameManager.Instance.CurrentPlayer == aiPlayer)
        // {
        //     yield return aiManager.StartCoroutine(RollDice(aiPlayer));
        // }
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
        
        bool resolved = false;
        void OnApplied(EventCard appliedCard)
        {
            if (appliedCard == card)
                resolved = true;
        }

        _eventManager.OnEventApplied += OnApplied; // 🔹 subscribe BEFORE ApplyEvent
        
        // if (ShouldSaveEvent(aiPlayer, card) && _mainPhase.TrySaveEvent(card))
        // {
        //     resolved = true;
        // }
        // else
        // {
        //     _mainPhase.EventManager.ApplyEvent(aiPlayer, card);
        // }
        
        //TODO:Don't forget to remove
        _mainPhase.EventManager.ApplyEvent(aiPlayer, card);
        
        
        // Wait until UI and game logic both finish
        yield return new WaitUntil(() => resolved);

        _eventManager.OnEventApplied -= OnApplied;
    }

    /// <summary>
    /// Determines if the AI should save or play the event card.
    /// </summary>
    private bool ShouldSaveEvent(AIPlayer aiPlayer, EventCard card)
    {
        // Example logic:
        // 🔹 Save cards that can provide extra rolls or depend on conditions not yet met.
        // 🔹 Use cards that can apply immediate benefits.
        if (!card.canSave) 
            return false;
        
        switch (card.subType)
        {
            case EventSubType.ExtraRoll_IfHasInstitution:
                // If AI doesn't have the required institution, save for later
                return !aiPlayer.HasInstitution(card.requiredInstitution);

            case EventSubType.ExtraRoll_Any:
                // Always beneficial now
                return false;
            
            case EventSubType.None:
                // If AI doesn't have the beneficial team, save for later
                return card.benefitingTeam != aiPlayer.assignedActor.team;

            default:
                // Randomized fallback to keep behavior less predictable
                return Random.value > 0.7f;
        }
    }
}