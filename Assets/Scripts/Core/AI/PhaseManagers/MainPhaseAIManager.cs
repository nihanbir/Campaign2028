using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class MainPhaseAIManager
{
    private readonly AIManager _aiManager;
    private GM_MainPhase _gmMainPhase;
    private EventManager _eventManager;

    public MainPhaseAIManager(AIManager manager)
    {
        _aiManager = manager;
    }

    public void InitializeAIManager()
    {
        _eventManager = _gmMainPhase.EventManager;
    }

#region Regular Turn Execution

    public IEnumerator ExecuteAITurn(AIPlayer aiPlayer, EventCard card)
    {
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));

        //TODO: Think delay or future logic (use saved events, modifiers, etc.)

        if (card != null)
        {
            yield return _aiManager.StartCoroutine(HandleEventCard(aiPlayer, card));
        }
        
        if (GameManager.Instance.CurrentPlayer == aiPlayer && !_eventManager.IsEventActive)
        {
            MainPhaseUIManager mainUI = GameUIManager.Instance.mainUI;
            yield return _aiManager.StartCoroutine(RollDice(aiPlayer, mainUI));
        }
        else
        {
            Debug.Log($"AI Player: {aiPlayer.playerID} lost its turn or challenge active");
        }
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
        
        if (!(ShouldSaveEvent(aiPlayer, card) && _gmMainPhase.TrySaveEvent(card)))
        {
            _gmMainPhase.EventManager.ApplyEvent(aiPlayer, card);
        }
        else
        {
            resolved = true;
        }
        
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
        
        
        switch (card.eventConditions)
        {
            case EventConditions.IfOwnsInstitution:
                // If AI doesn't have the required institution, save for later
                return !aiPlayer.HasInstitution(card.requiredInstitution);
            
            case EventConditions.IfInstitutionCaptured:
                // If none captured the institution yet, save for later
                _gmMainPhase.FindHeldInstitution(card.requiredInstitution, out var found);
                return !found;

            case EventConditions.None:
                // Always beneficial now
                return false;
            
            case EventConditions.TeamConditions:
                // If AI doesn't have the beneficial team, save for later
                return card.benefitingTeam != aiPlayer.assignedActor.team;
            
            case EventConditions.Any:
                // If other players don't have states, save for later
                return !AreOtherPlayersHoldingStates(aiPlayer);

            default:
                // Randomized fallback to keep behavior less predictable
                return Random.value > 0.7f;
        }
    }

    private bool AreOtherPlayersHoldingStates(AIPlayer aiPlayer)
    {
        var stateOwners = _gmMainPhase.GetStateOwners();
    
        // Return true if there are any held states and not all are by the given aiPlayer
        return stateOwners.Count > 0 && stateOwners.Values.Any(player => player != aiPlayer);
    }
    
    public IEnumerator RollDice(AIPlayer aiPlayer, MonoBehaviour uiManager)
    {
        Debug.Log("Ai is rolling");
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));

        switch (uiManager)
        {
            case MainPhaseUIManager mainUI:
                mainUI.OnRollDiceClicked();
                break;
            
            case ChallengeStateUIManager challengeUI:
                challengeUI.OnRollDiceClicked();
                break;
            
            case AlternativeStatesUIManager altUI:
                altUI.OnRollDiceClicked();
                break;
        }
    }
    
#endregion Regular Turn Execution
    
    
#region Challenge Any State
    public IEnumerator ExecuteChooseState(AIPlayer aiPlayer, List<StateCard> statesToChooseFrom)
    {
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));

        var chosenState = GetBestAvailableState(aiPlayer, statesToChooseFrom);
        
        _eventManager.HandleStateChosen(chosenState);

    }

    private StateCard GetBestAvailableState(AIPlayer aiPlayer, List<StateCard> statesToChooseFrom)
    {
        // Collect beneficial states
        List<StateCard> beneficialStates = new();
        foreach (var state in statesToChooseFrom)
        {
            if (state.benefitingTeam == aiPlayer.assignedActor.team)
                beneficialStates.Add(state);
        }

        // Pick the pool to choose from
        List<StateCard> selectionPool = beneficialStates.Count > 0 ? beneficialStates : statesToChooseFrom;

        // Select the one with the highest electoral votes
        StateCard chosenState = null;
        int highestVotes = 0;

        for (int i = 0; i < selectionPool.Count; i++)
        {
            var state = selectionPool[i];
            
            if (state.electoralVotes <= highestVotes) continue;
            
            highestVotes = state.electoralVotes;
            chosenState = state;
        }

        return chosenState;
    }
    
#endregion Challenge Any State
    
}