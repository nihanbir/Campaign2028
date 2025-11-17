using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AMP_EventResponse
{
    private readonly AIManager _ai;
    private readonly AM_MainPhase _main;
    private EUM_ChallengeEvent _ui;
    
    public AMP_EventResponse(AIManager ai, AM_MainPhase main)
    {
        _ai = ai;
        _main = main;
    }

    public void Enable()
    {
        EventCardBus.Instance.OnEvent += OnEvent;
        Debug.Log("Enabled");
        if (!_ui) _ui = GameUIManager.Instance.mainUI.eventUI;
    }

    private void Disable()  => EventCardBus.Instance.OnEvent -= OnEvent;
    
    private void OnEvent(EventCardEvent e)
    {
        // Only reacts to events, never active logic
        switch (e.stage)
        {
            case EventStage.EventCanceled:
                Disable();
                break;
        
            case EventStage.DuelCompleted:
                Disable();
                break;
            
            case EventStage.EventCompleted:
                Disable();
                break;
        
            case EventStage.ChallengeStatesDetermined:
            {
                var data = (ChallengeStatesData)e.payload;
                var aiPlayer = AIManager.Instance.GetAIPlayer(data.Player);
                _ai.StartCoroutine(ExecuteChooseState(aiPlayer, data.States));
                break;
            }

            case EventStage.DuelStarted:
            {
                var data = (DuelData)e.payload;
                var aiPlayer = AIManager.Instance.GetAIPlayer(data.Attacker);
                _ai.StartCoroutine(RollDiceForEvent(aiPlayer));
                break;
            }

            case EventStage.AltStatesShown:
            {
                var data = (AltStatesData)e.payload;
                var aiPlayer = AIManager.Instance.GetAIPlayer(data.Player);
                _ai.StartCoroutine(RollDiceForEvent(aiPlayer));
                break;
            }
        }
       
    }
    
    #region Challenge Any State (AI choice)
    private IEnumerator ExecuteChooseState(AIPlayer aiPlayer, List<StateCard> statesToChooseFrom)
    {
        yield return _ui.WaitUntilQueueFree();
        
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));

        var chosenState = GetBestAvailableState(aiPlayer, statesToChooseFrom);

        Debug.Log($"{chosenState.cardName} chosen by {aiPlayer.playerID}");
        
        SelectableCardBus.Instance.Raise(
            new CardInputEvent(CardInputStage.Held, chosenState));
    }

    private IEnumerator RollDiceForEvent(AIPlayer aiPlayer)
    {
        yield return _ui.WaitUntilQueueFree();
        
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));
        
        EventCardBus.Instance.Raise(
            new EventCardEvent(EventStage.RollDiceRequest));
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