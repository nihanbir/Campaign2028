using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MP_EventResponse
{
    private readonly AIManager _ai;
    private readonly AM_MainPhase _main;
    
    public MP_EventResponse(AIManager ai, AM_MainPhase main)
    {
        _ai = ai;
        _main = main;
    }

    private void Enable()   => EventCardBus.Instance.OnEvent += OnEvent;
    private void Disable()  => EventCardBus.Instance.OnEvent -= OnEvent;
    
    private void OnEvent(CardEvent e)
    {
        // Only reacts to events, never active logic
        switch (e.stage)
        {
            case EventStage.EventCanceled:
                Disable();
                break;
            
            case EventStage.EventCompleted:
                Disable();
                break;
            
            case EventStage.ChallengeStateShown:
            {
                var data = (ChallengeStatesData)e.Payload;
                // if AI is the current player, choose a state
                if (AIManager.Instance.IsAIPlayer(data.Player))
                {
                    var aiPlayer = AIManager.Instance.GetAIPlayer(data.Player);
                    _ai.StartCoroutine(ExecuteChooseState(aiPlayer, data.States));
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
                    // _ai.StartCoroutine(RollDice(aiPlayer));
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
                    // _ai.StartCoroutine(RollDice(aiPlayer));
                }
                break;
            }
        }
    }
    
    #region Challenge Any State (AI choice)
    

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
    
    private IEnumerator ExecuteChooseState(AIPlayer aiPlayer, List<StateCard> statesToChooseFrom)
    {
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));

        var chosenState = GetBestAvailableState(aiPlayer, statesToChooseFrom);

        Debug.Log($"{chosenState.cardName} chosen by {aiPlayer.playerID}");
        
        //raise a bus
    }
    
    #endregion
    
    
    
}