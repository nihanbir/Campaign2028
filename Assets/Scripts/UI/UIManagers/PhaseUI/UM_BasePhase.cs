
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public abstract class UM_BasePhase : MonoBehaviour
{
    protected GameManager game;
    
    [Header("Buttons")]     
    public Button rollDiceButton;
    public Image diceImage;
    
    [Header("UI Animation Settings")]
    public float enterDuration = 0.6f;
    public Ease enterEase = Ease.OutBack;
    public Ease exitEase = Ease.InBack;
    
    public abstract GamePhase PhaseType { get; }

    protected bool isActive = false;

    protected bool isPlayerAI;
    
    private readonly Queue<IEnumerator> _uiQueue = new();
    private bool _uiQueueRunning;
    
    protected void EnqueueUI(IEnumerator routine)
    {
        _uiQueue.Enqueue(routine);
        if (!_uiQueueRunning)
            StartCoroutine(ProcessUIQueue());
    }

    private IEnumerator ProcessUIQueue()
    {
        _uiQueueRunning = true;

        while (_uiQueue.Count > 0)
        {
            yield return StartCoroutine(_uiQueue.Dequeue());
        }

        _uiQueueRunning = false;
        
        EnableDiceButton(true);
    }
    
    private void Awake()
    {
        game = GameManager.Instance;
        
        gameObject.SetActive(false);
        
        game.OnPhaseChanged += OnPhaseChanged;
        
    }
    private void OnDestroy()
    {
        UnsubscribeToPhaseEvents();
        if (game != null)
            game.OnPhaseChanged -= OnPhaseChanged;
    }
    
    private void OnPhaseChanged(GM_BasePhase newPhase)
    {
        if (PhaseType == newPhase.PhaseType)
            OnPhaseEnabled();
        else if (PhaseType != newPhase.PhaseType && isActive)
            OnPhaseDisabled();
    }

    protected virtual void OnPhaseEnabled()
    {
        GameUIManager.Instance.RegisterRollButtonAndDiceImage(rollDiceButton, diceImage);
        
        EnableDiceButton(false);
        
        SubscribeToPhaseEvents();
        
        gameObject.SetActive(true);
        
        isActive = true;
        
        // Animate UI entry
        EnqueueUI(AnimatePhaseEntry());
        
    }

    protected virtual void OnPhaseDisabled()
    {
        
        isActive = false;
        
        EnqueueUI(AnimatePhaseExit(() =>
        {
            // 🔥 Only run this AFTER animation is done
            UnsubscribeToPhaseEvents();
            gameObject.SetActive(false);
        }));
    }

    protected virtual void HandleTurnEvent(IGameEvent e)
    {
        if (!isActive) return;
        if (e is TurnEvent t)
        {
            switch (t.stage)
            {
                case TurnStage.PlayerRolled:
                {
                    var data = (PlayerRolledData)t.payload;
                    OnPlayerRolledDice(data.Player, data.Roll);
                    break;
                }
                case TurnStage.PlayerTurnStarted:
                {
                    var data = (PlayerTurnStartedData)t.payload;
                    OnPlayerTurnStarted(data.Player);
                    break;
                }
                case TurnStage.PlayerTurnEnded:
                {
                    var data = (PlayerTurnEndedData)t.payload;
                    OnPlayerTurnEnded(data.Player);
                    break;
                }
            }
        }
        
    }

    protected virtual void SubscribeToPhaseEvents()
    {
        TurnFlowBus.Instance.OnEvent += HandleTurnEvent;
    }

    protected virtual void UnsubscribeToPhaseEvents()
    {
        TurnFlowBus.Instance.OnEvent -= HandleTurnEvent;
    }
    
    protected virtual void OnPlayerTurnStarted(Player player)
    {
        isPlayerAI = AIManager.Instance.IsAIPlayer(player);
        
        var card = player.PlayerDisplayCard;
        if (card)
            EnqueueUI(card.HighlightRoutine());
        
    }
    
    protected virtual void OnPlayerTurnEnded(Player player)
    {
        EnableDiceButton(false);
        
        var card = player.PlayerDisplayCard;
        if (card)
            EnqueueUI(card.RemoveHighlightRoutine());
    }
    
    protected virtual void OnPlayerRolledDice(Player player, int roll)
    {
        EnqueueUI(DicePopAnimation(diceImage));

        var playerDiceImage = player.PlayerDisplayCard.diceImage;
        EnqueueUI(DicePopAnimation(playerDiceImage));
        
    }
    

    protected virtual void EnableDiceButton(bool enable)
    {
        if (!rollDiceButton) return;
        if (isPlayerAI || _uiQueueRunning)
        {
            enable = false;
        }

        rollDiceButton.interactable = enable;
    }
    
    public IEnumerator WaitUntilUIQueueFree()
    {
        while (_uiQueueRunning)
            yield return null;
    }
    
    protected virtual IEnumerator AnimatePhaseEntry()
    {
        transform.localPosition += new Vector3(-1200f, 0f, 0f);

        bool done = false;
        
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOLocalMoveX(0f, enterDuration).SetEase(enterEase));
        seq.Join(transform.DOScale(1.02f, 0.3f).SetLoops(2, LoopType.Yoyo));

        seq.OnComplete(() => done = true);

        while (!done) yield return null;
        
    }
    
    protected virtual IEnumerator AnimatePhaseExit(System.Action onComplete)
    {
        bool finished = false;
        
        Sequence s = DOTween.Sequence();
        s.Append(transform.DOLocalMoveX(1200f, 0.5f).SetEase(exitEase));

        // Optional fade-out if CanvasGroup is present
        var cg = GetComponent<CanvasGroup>();
        if (cg) s.Join(cg.DOFade(0f, 0.4f));

        s.OnComplete(() =>
        {
            finished = true;
            if (cg) cg.alpha = 1f;
            transform.localScale = Vector3.one;
            onComplete?.Invoke();
        });
        
        while (!finished) yield return null;
    }
    
    private IEnumerator DicePopAnimation(Image diceImage)
    {
        if (!diceImage) yield break;

        diceImage.gameObject.SetActive(true);

        diceImage.transform.DOKill();
        diceImage.transform.localScale = Vector3.zero;

        bool done = false;

        diceImage.transform
            .DOScale(1f, 0.35f)
            .SetEase(Ease.OutBack)
            .OnComplete(() => done = true);

        while (!done)
            yield return null;
    }
    
}
