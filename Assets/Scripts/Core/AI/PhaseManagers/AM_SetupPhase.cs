using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AM_SetupPhase
{
    private readonly AIManager _aiManager;
    private GM_SetupPhase _setupPhase;
    private UM_SetupPhase _setupUI;

    public AM_SetupPhase(AIManager manager)
    {
        _aiManager = manager;
        _setupPhase = _aiManager.game.setupPhase;
        _setupUI = GameUIManager.Instance.setupUI;
    }
    
    public IEnumerator ExecuteAITurn(AIPlayer aiPlayer)
    {
        //Make sure they're assigned
        if (_setupPhase == null) _setupPhase = _aiManager.game.setupPhase;
        if (_setupUI == null) _setupUI = GameUIManager.Instance.setupUI;
        
        // Simulate thinking
        yield return new WaitForSeconds(1f);
        
        if (_setupPhase.CurrentStage == SetupStage.Roll ||
            _setupPhase.CurrentStage == SetupStage.Reroll)
        {
            yield return _aiManager.StartCoroutine(RollDice(aiPlayer));
        }
        else
        {
            yield return _aiManager.StartCoroutine(AssignActorToAnotherPlayer(aiPlayer));
        }
    }

    private IEnumerator RollDice(AIPlayer aiPlayer)
    {
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));
        _setupUI.OnRollDiceClicked();
    }

    private IEnumerator AssignActorToAnotherPlayer(AIPlayer aiPlayer)
    {
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));
        
        // Get all unassigned players except this AI
        var eligiblePlayers = _setupPhase.GetUnassignedPlayers()
            .Where(player => player != aiPlayer)
            .ToList();
            
        if (eligiblePlayers.Count == 0)
        {
            Debug.LogWarning($"AI Player {aiPlayer.playerID}: No eligible players to assign actor to!");
            yield break;
        }
        
        var availableActors = _setupPhase.GetUnassignedActors();
        if (availableActors.Count == 0)
        {
            Debug.LogWarning($"AI Player {aiPlayer.playerID}: No available actors to assign!");
            yield break;
        }
        
        int actorIndex = Random.Range(0, availableActors.Count);
        int playerIndex = Random.Range(0, eligiblePlayers.Count);
    
        var selectedActor = availableActors[actorIndex];
        var selectedPlayer = eligiblePlayers[playerIndex];

         var actorDisplay = _setupUI.FindDisplayCardForUnassignedActor(selectedActor);
         var playerDisplay = _setupUI.FindDisplayCardForPlayer(selectedPlayer);
    
        Debug.Log($"AI Player {aiPlayer.playerID} assigning {selectedActor.cardName} to Player {selectedPlayer.playerID}");
    
        _setupUI.SelectActorCard(actorDisplay);
        _setupUI.AssignSelectedActorToPlayer(playerDisplay);
    }
    
}