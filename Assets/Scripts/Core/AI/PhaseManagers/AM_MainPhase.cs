using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// AI controller for Main Phase, refactored to listen to GameEventBus for event resolution,
/// while still using your existing UI hooks for spawning/rolling to preserve animations.
/// </summary>
public class AM_MainPhase
{
    private readonly AIManager _aiManager;
    private GM_MainPhase _mainPhase;
    private UM_MainPhase _mainUI;
    private EventManager _eventManager;

    private bool _eventResolvedWait;
    

    public AM_MainPhase(AIManager manager)
    {
        _aiManager  = manager;
        _mainPhase  = _aiManager.game.mainPhase;
        _mainUI     = GameUIManager.Instance.mainUI;

        GameEventBus.Instance.OnEvent += OnBusEvent;
    }

    private void OnBusEvent(GameEvent e)
    {
        switch (e.stage)
        {
            case EventStage.EventCompleted:
                _eventResolvedWait = true;
                break;
            
            case EventStage.ChallengeStateShown:
            {
                var data = (ChallengeStatesData)e.Payload;
                // if AI is the current player, choose a state
                if (AIManager.Instance.IsAIPlayer(data.Player))
                {
                    var aiPlayer = AIManager.Instance.GetAIPlayer(data.Player);
                    _aiManager.StartCoroutine(ExecuteChooseState(aiPlayer, data.States));
                }
                break;
            }

            case EventStage.DuelStarted:
            {
                var data = (DuelData)e.Payload;
                // if AI is the attacker, roll dice
                if (AIManager.Instance.IsAIPlayer(data.Attacker))
                {
                    var aiPlayer = AIManager.Instance.GetAIPlayer(data.Attacker);
                    _aiManager.StartCoroutine(RollDice(aiPlayer));
                }
                break;
            }

            case EventStage.AltStatesShown:
            {
                var data = (AltStatesData)e.Payload;
                // if AI is the player, roll dice for alternative states
                if (AIManager.Instance.IsAIPlayer(data.Player))
                {
                    var aiPlayer = AIManager.Instance.GetAIPlayer(data.Player);
                    _aiManager.StartCoroutine(RollDice(aiPlayer));
                }
                break;
            }
        }
    }

    #region Regular Turn Execution
    public IEnumerator ExecuteAITurn(AIPlayer aiPlayer)
    {
        if (_mainPhase == null)  _mainPhase  = GameManager.Instance.mainPhase;
        if (_mainUI == null)     _mainUI     = GameUIManager.Instance.mainUI;
        if (_eventManager == null) _eventManager = _mainPhase.EventManager;

        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));
        
        if (_mainPhase.CurrentTargetCard == null)
        {
            // Keep UI animation/flow you already have
            _mainUI.OnSpawnTargetClicked();
        }
        
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));
        
        _mainUI.OnSpawnEventClicked();
        
        if (_mainPhase.CurrentEventCard != null)
        {
            yield return _aiManager.StartCoroutine(HandleEventCard(aiPlayer, _mainPhase.CurrentEventCard));
        }
        
        // if (GameManager.Instance.CurrentPlayer == aiPlayer && !_eventManager.IsEventActive)
        // {
        //     yield return _aiManager.StartCoroutine(RollDice(aiPlayer));
        // }
        // else
        // {
        //     Debug.Log($"AI Player: {aiPlayer.playerID} lost its turn or challenge active");
        // }
    }
    
    private IEnumerator HandleEventCard(AIPlayer aiPlayer, EventCard card)
    {
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));

        _eventResolvedWait = false;

        // Decide to save or apply; keep UI calls to preserve your animations
        //TODO: don't forget to remove
        // if (ShouldSaveEvent(aiPlayer, card))
        // {
        //     if (_mainPhase.TrySaveEvent(card))
        //     {
        //         _eventResolvedWait = true; // saving resolves immediately for AI flow
        //     }
        // }
        // else
        // {

            Debug.Log($"{aiPlayer.playerID} is handling event, current player is {GameManager.Instance.CurrentPlayer.playerID} ");  
            _mainPhase.EventManager.ApplyEvent(aiPlayer, card);
        // }

        // Wait until the EventManager broadcasts completion (or save resolved)
        // yield return new WaitUntil(() => _eventResolvedWait);
    }

    private bool ShouldSaveEvent(AIPlayer aiPlayer, EventCard card)
    {
        if (!card.canSave) return false;
        if (aiPlayer.HeldEvent != null) return false;

        switch (card.eventConditions)
        {
            case EventConditions.IfOwnsInstitution:
                return !aiPlayer.HasInstitution(card.requiredInstitution);
            
            case EventConditions.IfInstitutionCaptured:
                _mainPhase.FindHeldInstitution(card.requiredInstitution, out var found);
                return !found;

            case EventConditions.None:
                return false;
            
            case EventConditions.TeamConditions:
                return card.benefitingTeam != aiPlayer.assignedActor.team;
            
            case EventConditions.Any:
                return !AreOtherPlayersHoldingStates(aiPlayer);

            default:
                return Random.value > 0.7f;
        }
    }

    private bool AreOtherPlayersHoldingStates(AIPlayer aiPlayer)
    {
        var stateOwners = _mainPhase.GetStateOwners();
        return stateOwners.Count > 0 && stateOwners.Values.Any(player => player != aiPlayer);
    }
    
    public IEnumerator RollDice(AIPlayer aiPlayer)
    {
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));

        int roll = Random.Range(1, 7);
        GameUIManager.Instance.DiceRoll = roll;

        GameEventBus.Instance.Raise(
            new GameEvent(EventStage.RollDiceRequest, new PlayerRolledData(aiPlayer, roll))
        );
    }
    #endregion

    #region Challenge Any State (AI choice)
    public IEnumerator ExecuteChooseState(AIPlayer aiPlayer, List<StateCard> statesToChooseFrom)
    {
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));

        var chosenState = GetBestAvailableState(aiPlayer, statesToChooseFrom);

        Debug.Log($"{chosenState.cardName} chosen by {aiPlayer.playerID}");
        // Feed the choice back to logic; UI will have been opened by bus already
        _mainPhase.EventManager.HandleStateChosen(chosenState);
    }

    private StateCard GetBestAvailableState(AIPlayer aiPlayer, List<StateCard> statesToChooseFrom)
    {
        List<StateCard> beneficialStates = new();
        foreach (var state in statesToChooseFrom)
        {
            if (state.benefitingTeam == aiPlayer.assignedActor.team)
                beneficialStates.Add(state);
        }

        var pool = beneficialStates.Count > 0 ? beneficialStates : statesToChooseFrom;

        StateCard chosen = null;
        int highestVotes = -1;
        for (int i = 0; i < pool.Count; i++)
        {
            var s = pool[i];
            if (s.electoralVotes > highestVotes)
            {
                highestVotes = s.electoralVotes;
                chosen = s;
            }
        }
        return chosen;
    }
    #endregion
}
