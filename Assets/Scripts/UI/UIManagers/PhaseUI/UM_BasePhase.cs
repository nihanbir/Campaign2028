
using System.Collections;
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
    
    public System.Action OnUIReady;
    
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
        AnimatePhaseEntry();
        
    }

    protected virtual void OnPhaseDisabled()
    {
        isActive = false;
        
        AnimatePhaseExit(() =>
        {
            // 🔥 Only run this AFTER animation is done
            UnsubscribeToPhaseEvents();
            gameObject.SetActive(false);
        });
    }

    protected virtual void SubscribeToPhaseEvents() { }
    
    protected virtual void UnsubscribeToPhaseEvents() { }
    
    protected virtual void OnPlayerTurnStarted(Player player)
    {
        isPlayerAI = AIManager.Instance.IsAIPlayer(player);
        
        player.PlayerDisplayCard.Highlight();
        
    }
    
    protected virtual void OnPlayerTurnEnded(Player player)
    {
        EnableDiceButton(false);
        
        player.PlayerDisplayCard.RemoveHighlight();
    }
    
    protected virtual void OnPlayerRolledDice(Player player, int roll)
    {
        
    }
    

    protected virtual void EnableDiceButton(bool enable)
    {
        if (!rollDiceButton) return;
        if (isPlayerAI)
        {
            enable = false;
        }

        rollDiceButton.interactable = enable;
    }
    
    protected virtual void AnimatePhaseEntry()
    {
        transform.localPosition += new Vector3(-1200f, 0f, 0f);

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOLocalMoveX(0f, enterDuration).SetEase(enterEase));
        seq.Join(transform.DOScale(1.02f, 0.3f).SetLoops(2, LoopType.Yoyo));

        seq.OnComplete(() => OnUIReady?.Invoke());

    }
    
    protected virtual void AnimatePhaseExit(System.Action onComplete)
    {
        Sequence s = DOTween.Sequence();
        s.Append(transform.DOLocalMoveX(1200f, 0.5f).SetEase(exitEase));

        // Optional fade-out if CanvasGroup is present
        var cg = GetComponent<CanvasGroup>();
        if (cg) s.Join(cg.DOFade(0f, 0.4f));

        s.OnComplete(() =>
        {
            if (cg) cg.alpha = 1f;
            transform.localScale = Vector3.one;
            onComplete?.Invoke();
        });
    }

    protected virtual void DicePopInAnimation()
    {
        if (rollDiceButton.gameObject.activeSelf)
        {
            if (!diceImage.gameObject.activeSelf)
            {
                diceImage.gameObject.SetActive(true);
            }
            diceImage.transform.localScale = Vector3.zero;
            diceImage.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        }
    }
    
}
