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
    private GM_SetupPhase _setupPhase;

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
        base.HandleTurnEvent(e);
        if (e is SetupStageEvent t)
        {
            switch (t.stage)
            {
                case SetupStage.Roll:
                    rollDiceButton.gameObject.SetActive(true);
                    diceImage.gameObject.SetActive(true);
                    break;
                
                case SetupStage.AllPlayersRolled:
                    AnimateAllDicePopAndThenProcess();
                    break;
                
                case SetupStage.UniqueWinner:
                    var uniqueWinner = (UniqueWinner)t.payload;
                    AnimateWinner(uniqueWinner.player);
                    break;
                
                case SetupStage.TiedRoll:
                    var tiedRoll = (TiedRoll)t.payload;
                    AnimateRerollPlayers(tiedRoll.players);
                    break;
                
                case SetupStage.BeginActorAssignment:
                    OnActorAssignStage();
                    break;
                
                case SetupStage.ActorAssigned:
                    var assigned = (ActorAssigned)t.payload;
                    UpdatePlayerUIWithActor(assigned.player, assigned.actor);
                    break;
                
                case SetupStage.LastActorAssigned:
                    //TODO:
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
                        HighlightActorCard(a);
                    }
                    break;
            }
        }
    }

    protected override void OnPhaseEnabled()
    {
        _setupPhase = game.setupPhase;
        
        CreateCardUI(CardDisplayType.UnassignedActor, actorUIParent);
        CreateCardUI(CardDisplayType.UnassignedPlayer, playerUIParent);
        
        base.OnPhaseEnabled();
    }
    
    void CreateCardUI(CardDisplayType cardType, Transform parent)
    {
        if (!GameManager.Instance)
        {
            Debug.LogError("GameManager instance is not set");
            return;
        }

        int count = cardType switch
        {
            CardDisplayType.UnassignedActor => _setupPhase.GetUnassignedActors().Count,
            CardDisplayType.UnassignedPlayer => _setupPhase.GetUnassignedPlayers().Count,
            _ => 0
        };

        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            GameObject uiInstance = Instantiate(cardDisplayPrefab, parent);
            PlayerDisplayCard displayCard = uiInstance.GetComponent<PlayerDisplayCard>();
            
            if (displayCard)
            {
                displayCard.displayType = cardType;
                
                if (cardType == CardDisplayType.UnassignedActor)
                    displayCard.SetCard(_setupPhase.GetUnassignedActors()[i]);
                else
                {
                    _setupPhase.GetUnassignedPlayers()[i].SetDisplayCard(displayCard);
                    _playerDisplayCards.Add(displayCard);
                }
            }
            else
                Debug.LogError("CardDisplayPrefab missing DisplayCard component.");
        }
    }
    
    #endregion
    
    #region Turn State UI

    protected override void OnPlayerTurnStarted(Player player)
    {
        base.OnPlayerTurnStarted(player);
        
        EnableDiceButton(true);
        
    }

    #endregion

    #region Actor Assignment UI
    
    private void OnActorAssignStage()
    {
        rollDiceButton.gameObject.SetActive(false);
        diceImage.gameObject.SetActive(false);
        _selectedActorCard = null;
    }
    
    private void HighlightActorCard(ActorCard actorCard)
    {
       
        if (_selectedActorCard)
            _selectedActorCard.SetIsSelected(false);
        
        _selectedActorCard = FindDisplayCardForUnassignedActor(actorCard);
        _selectedActorCard?.SetIsSelected(true);

        // Selection feedback
        _selectedActorCard?.transform.DOPunchScale(Vector3.one * 0.15f, 0.25f, 8, 1f);

        Debug.Log($"Selected actor: {_selectedActorCard?.GetCard().cardName}");
    }
    
    private void UpdatePlayerUIWithActor(Player player, ActorCard actor)
    { 
        Debug.Log($"Auto-assigning last actor {actor.cardName} to Player {player.playerID}");
    
        var actorCard = FindDisplayCardForUnassignedActor(actor);
        var playerCard = FindDisplayCardForPlayer(player);
        
        AnimateActorAssignment(actorCard.transform, playerCard.transform);

        DOVirtual.DelayedCall(assignDuration, () =>
        {
            RemoveCard(actorCard);
            playerCard.ConvertToAssignedActor(actor);
        });
    }
    
    private void RemoveCard(PlayerDisplayCard card)
    {
        Destroy(card.gameObject);
    }
    
    public PlayerDisplayCard FindDisplayCardForPlayer(Player player)
    {
        foreach (Transform child in playerUIParent)
        {
            var display = child.GetComponent<PlayerDisplayCard>();
            if (!display) return null;
            if (display.owningPlayer == player)
                return display;
        }
        return null;
    }
    
    public PlayerDisplayCard FindDisplayCardForUnassignedActor(ActorCard actor)
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
    
    private void AnimateAllDicePopAndThenProcess()
    {
        Debug.Log("🎲 Animating all dice results...");
        
        Sequence group = DOTween.Sequence();
        
        foreach (var card in _playerDisplayCards)
        {
            // Each card gets its own looping pulse sequence
            var diceObj = card.GetDiceTransform();
            if (diceObj != null)
            {
                Sequence pulse = DOTween.Sequence();
                pulse.Append(diceObj.DOPunchScale(Vector3.one * 0.3f, 0.4f, 6, 1).SetEase(Ease.OutBack));
                
                group.Join(pulse);
                
            }
        }
        
        group.OnComplete(() =>
        {
            Debug.Log("🎬 Dice animation complete — resuming game logic");
            
            //TODO: move this
            // _setupPhase.ProcessRollResults();
        });
        
    }

    private void AnimateActorAssignment(Transform actor, Transform target)
    {
        Vector3 targetPos = target.position;
        Sequence s = DOTween.Sequence();
        s.Append(actor.DOMove(targetPos, assignDuration).SetEase(Ease.InBack));
        s.Join(actor.DOScale(1.3f, assignDuration * 0.5f).SetLoops(2, LoopType.Yoyo));
    }
    
    private void AnimateRerollPlayers(List<Player> rerollingPlayers)
    {
        // Create a master sequence
        Sequence group = DOTween.Sequence();

        foreach (var player in rerollingPlayers)
        {
            var card = FindDisplayCardForPlayer(player);
            if (card == null) continue;

            var pulse = PulsateCardDice(card);

            // Add this sequence to the group so they play in parallel
            group.Join(pulse);
        }
        
        // 🔥 When all pulses are done, handle tied roll
        group.OnComplete(() =>
        {
            Debug.Log("All reroll animations done");
            HideCardDices(true);
        });
    }

    private void AnimateWinner(Player winner)
    {
        var card = FindDisplayCardForPlayer(winner);
        if (card == null) return;

        var s = PulsateCardDice(card);
        
        // 🔥 Wait until the animation is fully done
        s.OnComplete(() =>
        {
            HideCardDices(true);
        });
    }

    private Sequence PulsateCardDice(PlayerDisplayCard card)
    {
        var t = card.transform;

        // Flash + scale pulse to draw attention
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
