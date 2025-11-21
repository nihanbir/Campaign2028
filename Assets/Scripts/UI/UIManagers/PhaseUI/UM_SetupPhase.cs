using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class UM_SetupPhase : UM_BasePhase
{
    public override GamePhase PhaseType => GamePhase.Setup;
    
    [Header("Card Display")]
    public GameObject cardDisplayPrefab;
    public Transform playerUIParent;
    public Transform actorUIParent;
    
    private PlayerDisplayCard _highlightedCard;
    private readonly List<PlayerDisplayCard> _playerDisplayCards = new();
    private PlayerDisplayCard _selectedActorCard;

    [Header("UI Animation Settings")]
    public float assignDuration = 0.5f;
    
    [Header("Dice Animation Settings")]
    public float dicePopScale = 1.4f;
    public float dicePopDuration = 0.3f;
    public Ease dicePopEase = Ease.OutBack;

    [Header("Reroll Animation Settings")]
    public float rerollPulseDuration = 0.5f;
    public Ease rerollEase = Ease.InOutSine;

    #region Initialize Phase UI
    
    protected override void HandleTurnEvent(IGameEvent e)
    {
        // UI ALWAYS handles events (both offline and online)
        base.HandleTurnEvent(e);

        if (!isCurrent) return;
        
        if (e is SetupStageEvent t)
        {
            switch (t.stage)
            {
                case SetupStage.BeginPhase:
                    var data = (BeginPhaseData)t.payload;
                    CreateCardUI(data.unassignedPlayers, data.unassignedActors);
                    break;
                
                case SetupStage.Roll:
                    EnqueueUI(OnRollStage());
                    break;
                
                case SetupStage.AllPlayersRolled:
                    EnqueueUI(AnimateAllDicePopAndThenProcess());
                    break;
                
                case SetupStage.UniqueWinner:
                    var uniqueWinner = (UniqueWinnerData)t.payload;
                    EnqueueUI(AnimateWinner(uniqueWinner.player));
                    break;
                
                case SetupStage.TiedRoll:
                    var tiedRoll = (TiedRollData)t.payload;
                    EnqueueUI(AnimateRerollPlayers(tiedRoll.players));
                    break;
                
                case SetupStage.BeginActorAssignment:
                    EnqueueUI(OnActorAssignStage());
                    break;
                
                case SetupStage.ActorAssigned:
                    var assigned = (ActorAssignedData)t.payload;
                    EnqueueUI(UpdatePlayerUIWithActor(assigned.player, assigned.actor));
                    break;
                
                case SetupStage.LastActorAssigned:
                    break;
            }
        }
        else if (e is CardInputEvent c)
        {
            switch (c.stage)
            {
                case CardInputStage.Clicked:
                    if (c.payload is ActorCard a)
                    {
                        EnqueueUI(HighlightActorCard(a));
                    }
                    break;
            }
        }
    }

    private void CreateCardUI(List<Player> unassignedPlayers, List<ActorCard> unassignedActors)
    {
        if (!GameManager.Instance)
        {
            Debug.LogError("GameManager instance is not set");
            return;
        }
        
        for (int i = 0; i < unassignedActors.Count; i++)
        {
            GameObject uiInstance = Instantiate(cardDisplayPrefab, actorUIParent);
            PlayerDisplayCard displayCard = uiInstance.GetComponent<PlayerDisplayCard>();
                
            if (displayCard)
            {
                displayCard.displayType = CardDisplayType.UnassignedActor;
                displayCard.SetCard(unassignedActors[i]);
            }
            else
                Debug.LogError("CardDisplayPrefab missing DisplayCard component.");
        }
        
        for (int i = 0; i < unassignedPlayers.Count; i++)
        {
            GameObject uiInstance = Instantiate(cardDisplayPrefab, playerUIParent);
            PlayerDisplayCard displayCard = uiInstance.GetComponent<PlayerDisplayCard>();
                
            if (displayCard)
            {
                unassignedPlayers[i].SetDisplayCard(displayCard);
                _playerDisplayCards.Add(displayCard);
            }
            else
                Debug.LogError("CardDisplayPrefab missing DisplayCard component.");
        }
        
        InitUI();
    }
    
    #endregion
    
    #region Turn State UI

    private IEnumerator OnRollStage()
    {
        rollDiceButton.gameObject.SetActive(true);
        diceImage.gameObject.SetActive(true);
        yield break;
    }
    
    protected override void OnPlayerTurnStarted(Player player)
    {
        base.OnPlayerTurnStarted(player);
        
        EnqueueUI(EnableDiceButtonRoutine(!isAIPlayer));
    }

    protected override void OnPlayerRolledDice(Player player, int roll)
    {
        EnqueueUI(EnableDiceButtonRoutine(false));
        base.OnPlayerRolledDice(player, roll);
    }

    #endregion

    #region Actor Assignment UI
    
    private IEnumerator OnActorAssignStage()
    {
        rollDiceButton.gameObject.SetActive(false);
        diceImage.gameObject.SetActive(false);
        _selectedActorCard = null;
        yield break;
    }
    
    private IEnumerator HighlightActorCard(ActorCard actorCard)
    {
        if (_selectedActorCard)
            _selectedActorCard.SetIsSelected(false);
        
        _selectedActorCard = FindDisplayCardForUnassignedActor(actorCard);
        _selectedActorCard?.SetIsSelected(true);

        _selectedActorCard?.transform.DOPunchScale(Vector3.one * 0.15f, 0.25f, 8, 1f);

        Debug.Log($"Selected actor: {_selectedActorCard?.GetCard().cardName}");
        yield break;
    }
    
    private void RemoveCard(PlayerDisplayCard card)
    {
        Destroy(card.gameObject);
    }

    private PlayerDisplayCard FindDisplayCardForPlayer(Player player)
    {
        foreach (Transform child in playerUIParent)
        {
            var display = child.GetComponent<PlayerDisplayCard>();
            if (!display) continue;
            if (display.owningPlayer == player)
                return display;
        }
        return null;
    }

    private PlayerDisplayCard FindDisplayCardForUnassignedActor(ActorCard actor)
    {
        foreach (Transform child in actorUIParent)
        {
            var display = child.GetComponent<PlayerDisplayCard>();
            if (!display) continue;
            if (display.GetCard() == actor)
                return display;
        }
        return null;
    }

    #endregion

    #region Animations
    
    private IEnumerator AnimateAllDicePopAndThenProcess()
    {
        Debug.Log("🎲 Animating all dice results...");
        
        bool done = false;
        
        Sequence group = DOTween.Sequence();
        
        foreach (var card in _playerDisplayCards)
        {
            var diceObj = card.GetDiceTransform();
            if (diceObj == null) continue;
            
            Sequence pulse = DOTween.Sequence();
            pulse.Append(diceObj.DOPunchScale(Vector3.one * 0.3f, 0.4f, 6, 1).SetEase(Ease.OutBack));
                
            group.Join(pulse);
        }
        
        group.OnComplete(() => done = true);

        while (!done)
            yield return null;
    }
    
    private IEnumerator UpdatePlayerUIWithActor(Player player, ActorCard actor)
    { 
        Debug.Log($"Assigning actor {actor.cardName} to Player {player.playerID}");
    
        var actorCard = FindDisplayCardForUnassignedActor(actor);
        var playerCard = FindDisplayCardForPlayer(player);
        
        if (!actorCard || !playerCard) yield break;
        
        bool done = false;
        
        AnimateAssignment(actorCard.transform, playerCard.transform)
            .OnComplete(() => done = true);

        while (!done)
            yield return null;

        RemoveCard(actorCard);
        playerCard.ConvertToAssignedActor(actor);
    }
    
    private Sequence AnimateAssignment(Transform actor, Transform target)
    {
        Sequence s = DOTween.Sequence();
        s.Append(actor.DOMove(target.position, assignDuration).SetEase(Ease.InBack));
        s.Join(actor.DOScale(1.3f, assignDuration * 0.5f).SetLoops(2, LoopType.Yoyo));
        return s;
    }
    
    private IEnumerator AnimateRerollPlayers(List<Player> rerollingPlayers)
    {
        Sequence group = DOTween.Sequence();

        foreach (var player in rerollingPlayers)
        {
            var card = FindDisplayCardForPlayer(player);
            if (card == null) continue;

            var pulse = PulsateCardDice(card);
            group.Join(pulse);
        }
        
        bool finished = false;
        group.OnComplete(() => finished = true);

        while (!finished)
            yield return null;

        Debug.Log("All reroll animations done");

        HideCardDices(true);
    }

    private IEnumerator AnimateWinner(Player winner)
    {
        var card = FindDisplayCardForPlayer(winner);
        if (card == null) yield break;
        
        bool done = false;
        
        var seq = PulsateCardDice(card);
        seq.OnComplete(() =>
        {
            HideCardDices(true);
            done = true;
        });

        while (!done)
            yield return null;
    }

    private Sequence PulsateCardDice(PlayerDisplayCard card)
    {
        var t = card.transform;

        Sequence s = DOTween.Sequence();
        s.Append(t.DOScale(1.1f, rerollPulseDuration).SetEase(rerollEase));
        s.Append(t.DOScale(1f, rerollPulseDuration).SetEase(rerollEase));
        s.SetLoops(3, LoopType.Yoyo);

        return s;
    }

    private void HideCardDices(bool hide)
    {
        foreach (var card in _playerDisplayCards)
        {
            card.ShowDice(!hide);
        }
    }

    #endregion
}