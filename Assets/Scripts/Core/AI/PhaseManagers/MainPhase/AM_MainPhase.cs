using System.Collections;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// AI controller for Main Phase, refactored to listen to GameEventBus for event resolution,
/// while still using your existing UI hooks for spawning/rolling to preserve animations.
/// </summary>
public class AM_MainPhase
{
    private readonly AIManager _ai;
    private MP_EventResponse _aiEventManager;

    private GM_MainPhase _gm;
    private EventManager _eventManager;

    private AIPlayer _currentAI;

    private bool _eventResolvedWait;

    private EventCard _drawnEvent;
    private Card _drawnTarget;

    public AM_MainPhase(AIManager ai)
    {
        _ai = ai;
        _aiEventManager = new MP_EventResponse(ai, this);
    }
    
    private void OnTurnEvent(IGameEvent e)
    {
        if (e is MainStageEvent m)
        {
            switch (m.stage)
            {
                case MainStage.EventCardDrawn:
                    HandleEventCard(_currentAI, (EventCard)m.payload);
                    break;
                
                case MainStage.TargetCardDrawn:
                    HandleTargetDrawn(_currentAI, (Card)m.payload);
                    break;
            }
        }

        if (e is TurnEvent t)
        {
            if (t.stage == TurnStage.PlayerTurnEnded) Disable();
        }
    }

    #region Regular Turn Execution
    
    private void Enable()   => TurnFlowBus.Instance.OnEvent += OnTurnEvent;
    
    private void Disable()
    {
        _currentAI = null;
        _drawnTarget = null;
        _drawnEvent = null;
        TurnFlowBus.Instance.OnEvent -= OnTurnEvent;
    }
    
    public IEnumerator ExecuteAITurn(AIPlayer aiPlayer)
    {
        if (_gm == null)  _gm = GameManager.Instance.mainPhase;
        if (_eventManager == null) _eventManager = _gm.EventManager;
        
        Enable();
        
        _currentAI = aiPlayer;
        
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));
        
        if (_gm.CurrentTargetCard == null)
        {
            TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.DrawTargetCardRequest));
        }
        else
        {
            _drawnTarget = _gm.CurrentTargetCard;
        }
        
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));
        
        TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.DrawEventCardRequest));
        
        yield return _ai.StartCoroutine(WaitUntilCardsReady());
        
        if (GameManager.Instance.CurrentPlayer == aiPlayer && !_eventManager.IsEventActive)
        {
            TurnFlowBus.Instance.Raise(new TurnEvent(TurnStage.RollDiceRequest));
        }
        else
        {
            Debug.Log($"AI Player: {aiPlayer.playerID} lost its turn or challenge active");
        }
    }
    
    private void HandleEventCard(AIPlayer aiPlayer, EventCard card)
    {
        _drawnEvent = card;

        // If target not ready yet, wait for it
        if (_drawnTarget == null)
        {
            // Start a coroutine that waits for target and then applies
            _ai.StartCoroutine(WaitAndApplyEvent(aiPlayer, card));
            return;
        }

        ApplyOrSave(aiPlayer, card);
    }

    private void HandleTargetDrawn(AIPlayer aiPlayer, Card card)
    {
        _drawnTarget = card;
    }
    
    private IEnumerator WaitAndApplyEvent(AIPlayer ai, EventCard card)
    {
        // Wait until target exists
        while (_drawnTarget == null)
            yield return null;

        ApplyOrSave(ai, card);
    }

    private void ApplyOrSave(AIPlayer ai, EventCard card)
    {
        TurnFlowBus.Instance.Raise(
            ShouldSaveEvent(ai, card)
                ? new MainStageEvent(MainStage.SaveEventCardRequest)
                : new MainStageEvent(MainStage.ApplyEventCardRequest)
        );
    }
    
    private IEnumerator WaitUntilCardsReady()
    {
        // Wait until BOTH cards are present
        while (_drawnTarget == null || _drawnEvent == null)
            yield return null;
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
                _gm.FindHeldInstitution(card.requiredInstitution, out var found);
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
        var stateOwners = _gm.GetStateOwners();
        return stateOwners.Count > 0 && stateOwners.Values.Any(player => player != aiPlayer);
    }
    
    public IEnumerator RollDice(AIPlayer aiPlayer)
    {
        yield return new WaitForSeconds(Random.Range(aiPlayer.decisionDelayMin, aiPlayer.decisionDelayMax));
        
        TurnFlowBus.Instance.Raise(new TurnEvent(TurnStage.RollDiceRequest));
    }
    #endregion
}
