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
    private List<PlayerDisplayCard> _playerDisplayCards = new();
    private PlayerDisplayCard _selectedActorCard;
    private GM_SetupPhase _setupPhase;

    [Header("UI Animation Settings")]
    public float enterDuration = 0.6f;
    public float cardSpawnDuration = 0.4f;
    public float assignDuration = 0.5f;
    public Ease enterEase = Ease.OutBack;
    public Ease exitEase = Ease.InBack;
    
    [Header("Dice Animation Settings")]
    public float dicePopScale = 1.4f;
    public float dicePopDuration = 0.3f;
    public Ease dicePopEase = Ease.OutBack;

    [Header("Reroll Animation Settings")]
    public float rerollPulseDuration = 0.5f;
    public Ease rerollEase = Ease.InOutSine;

    #region Initialize Phase UI

    protected override void OnPhaseEnabled()
    {
        _setupPhase = game.setupPhase;

        // Animate UI entry
        AnimatePhaseEntry();

        CreateCardUI(CardDisplayType.UnassignedActor, actorUIParent);
        CreateCardUI(CardDisplayType.UnassignedPlayer, playerUIParent);
        
        base.OnPhaseEnabled();
    }

    protected override void OnPhaseDisabled()
    {
        DOTween.Kill(actorUIParent);
        DOTween.Kill(playerUIParent);
        DOTween.Kill(rollDiceButton.transform);

        AnimatePhaseExit(() => base.OnPhaseDisabled());
    }

    protected override void SubscribeToPhaseEvents()
    {
        _setupPhase.OnPlayerTurnStarted += OnPlayerTurnStarted;
        _setupPhase.OnPlayerTurnEnded += OnPlayerTurnEnded;
        _setupPhase.OnAllPlayersRolled += AnimateAllDicePopAndThenProcess;
        _setupPhase.OnActorAssignStage += OnActorAssignStage;
        _setupPhase.OnLastActorAssigned += UpdatePlayerUIWithActor;
        _setupPhase.OnTiedRoll += AnimateRerollPlayers;
        _setupPhase.OnUniqueWinner += AnimateWinner;
        
    }

    protected override void UnsubscribeToPhaseEvents()
    {
        base.UnsubscribeToPhaseEvents();
        if (_setupPhase == null) _setupPhase = game.setupPhase;
        
        _setupPhase.OnPlayerTurnStarted -= OnPlayerTurnStarted;
        _setupPhase.OnPlayerTurnEnded -= OnPlayerTurnEnded;
        _setupPhase.OnAllPlayersRolled -= AnimateAllDicePopAndThenProcess;
        _setupPhase.OnActorAssignStage -= OnActorAssignStage;
        _setupPhase.OnLastActorAssigned -= UpdatePlayerUIWithActor;
        _setupPhase.OnTiedRoll -= AnimateRerollPlayers;
        _setupPhase.OnUniqueWinner -= AnimateWinner;

    }

    private void OnActorAssignStage()
    {
        PlayerDisplayCard.OnCardSelected += SelectActorCard;
        PlayerDisplayCard.OnPlayerCardClicked += AssignSelectedActorToPlayer;
    }
    
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
            _setupPhase.ProcessRollResults();
        });
        
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

                //TODO: Use this in main
                // AnimateCardSpawn(uiInstance.transform, i * 0.05f);
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
        
        bool isAssignStage = GameManager.Instance.setupPhase.CurrentStage == SetupStage.AssignActor;
        rollDiceButton.gameObject.SetActive(!isAssignStage);

        // Button pop-in animation
        if (rollDiceButton.gameObject.activeSelf)
        {
            rollDiceButton.transform.localScale = Vector3.zero;
            rollDiceButton.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        }
    }

    public override void OnRollDiceClicked()
    {
        base.OnRollDiceClicked();

        // Roll button bounce feedback
        rollDiceButton.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.8f);

        var roll = GameUIManager.Instance.DiceRoll;
        _setupPhase.PlayerRolledDice(roll);
    }

    #endregion

    #region Actor Assignment UI
    
    public void SelectActorCard(ISelectableDisplayCard card)
    {
        var actorCard = card as PlayerDisplayCard;
        if (!actorCard)
        {
            Debug.Log($"no actor");
            return;
        }
        
        if (_selectedActorCard == actorCard)
            return;
    
        if (_selectedActorCard)
            _selectedActorCard.SetIsSelected(false);
    
        _selectedActorCard = actorCard;
        _selectedActorCard?.SetIsSelected(true);

        // Selection feedback
        _selectedActorCard.transform.DOPunchScale(Vector3.one * 0.15f, 0.25f, 8, 1f);

        Debug.Log($"Selected actor: {_selectedActorCard.GetCard().cardName}");
    }
    
    public void AssignSelectedActorToPlayer(PlayerDisplayCard playerCard)
    {
        if (_selectedActorCard == null)
        {
            Debug.LogWarning("No actor card selected to assign.");
            return;
        }
    
        ActorCard actorToAssign = _selectedActorCard.GetCard();

        if (!_setupPhase.TryAssignActorToPlayer(playerCard.owningPlayer, actorToAssign))
            return;

        // Animate actor card jumping to player
        AnimateActorAssignment(_selectedActorCard.transform, playerCard.transform);

        // Update UI after short delay
        DOVirtual.DelayedCall(assignDuration, () =>
        {
            RemoveCard(_selectedActorCard);
            playerCard.ConvertToAssignedActor(actorToAssign);
            _selectedActorCard = null;

            PlayerDisplayCard.OnCardSelected -= SelectActorCard;
            PlayerDisplayCard.OnPlayerCardClicked -= AssignSelectedActorToPlayer;
        });
    }
    
    private void UpdatePlayerUIWithActor(Player lastPlayer, ActorCard lastActor)
    { 
        Debug.Log($"Auto-assigning last actor {lastActor.cardName} to Player {lastPlayer.playerID}");
    
        var lastActorCard = FindDisplayCardForUnassignedActor(lastActor);
        var lastPlayerCard = FindDisplayCardForPlayer(lastPlayer);

        AnimateActorAssignment(lastActorCard.transform, lastPlayerCard.transform);

        DOVirtual.DelayedCall(assignDuration, () =>
        {
            RemoveCard(lastActorCard);
            lastPlayerCard.ConvertToAssignedActor(lastActor);
            _setupPhase.OnLastActorAssigned -= UpdatePlayerUIWithActor;
            _setupPhase.OnAllActorsAssigned();
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

    private void AnimatePhaseEntry()
    {
        actorUIParent.localPosition += new Vector3(-600f, 0, 0);
        playerUIParent.localPosition += new Vector3(600f, 0, 0);

        Sequence seq = DOTween.Sequence();
        seq.Join(actorUIParent.DOLocalMoveX(0f, enterDuration).SetEase(enterEase));
        seq.Join(playerUIParent.DOLocalMoveX(0f, enterDuration).SetEase(enterEase).SetDelay(0.1f));
        seq.Join(rollDiceButton.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetDelay(0.3f));
        
        seq.OnComplete(() =>
        {
            Debug.Log("✅ UI ready for SetupPhase");
            OnUIReady?.Invoke(); // <-- tell game logic we're done
        });
    }

    private void AnimatePhaseExit(System.Action onComplete)
    {
        Sequence s = DOTween.Sequence();
        s.Join(actorUIParent.DOLocalMoveX(-600f, 0.4f).SetEase(exitEase));
        s.Join(playerUIParent.DOLocalMoveX(600f, 0.4f).SetEase(exitEase));
        s.OnComplete(() => onComplete?.Invoke());
    }

    private void AnimateCardSpawn(Transform card, float delay)
    {
        card.localScale = Vector3.zero;
        card.DOScale(1f, cardSpawnDuration)
            .SetEase(Ease.OutBack)
            .SetDelay(delay)
            .SetUpdate(true);
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

            var t = card.transform;

            // Each card gets its own looping pulse sequence
            Sequence pulse = DOTween.Sequence();
            pulse.Append(t.DOScale(1.1f, rerollPulseDuration).SetEase(rerollEase));
            pulse.Append(t.DOScale(1f, rerollPulseDuration).SetEase(rerollEase));
            pulse.SetLoops(3, LoopType.Yoyo);

            // Add this sequence to the group so they play in parallel
            group.Join(pulse);
        }
        
        // 🔥 When all pulses are done, handle tied roll
        group.OnComplete(() =>
        {
            Debug.Log("All reroll animations done");
            HideCardDices(true);
            _setupPhase.HandleTiedRoll(rerollingPlayers);
        });
    }

    private void AnimateWinner(Player winner)
    {
        var card = FindDisplayCardForPlayer(winner);
        if (card == null) return;

        var t = card.transform;

        // Flash + scale pulse to draw attention
        Sequence s = DOTween.Sequence();
        s.Append(t.DOScale(1.1f, rerollPulseDuration).SetEase(rerollEase));
        s.Append(t.DOScale(1f, rerollPulseDuration).SetEase(rerollEase));
        s.SetLoops(3, LoopType.Yoyo);
        
        // 🔥 Wait until the animation is fully done
        s.OnComplete(() =>
        {
            HideCardDices(true);
            _setupPhase.HandleUniqueWinner(winner);
        });
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
