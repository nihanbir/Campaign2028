
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public abstract class UM_BasePhase : MonoBehaviour
{
    protected GameManager game;
    
    [Header("Phase Elements")]     
    public Button rollDiceButton;

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
        rollDiceButton.onClick.RemoveAllListeners();
        rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        
        EnableDiceButton(false);
        
        SubscribeToPhaseEvents();
        
        gameObject.SetActive(true);
        isActive = true;
    }

    protected virtual void OnPhaseDisabled()
    {
        rollDiceButton.onClick.RemoveAllListeners();
        
        UnsubscribeToPhaseEvents();
        
        gameObject.SetActive(false);
        isActive = false;
    }

    protected virtual void SubscribeToPhaseEvents() { }
    
    protected virtual void UnsubscribeToPhaseEvents() { }
    
    protected virtual void OnPlayerTurnStarted(Player player)
    {
        Debug.Log("is it ever here");
        isPlayerAI = AIManager.Instance.IsAIPlayer(player);
        
        EnableDiceButton(true);

        player.PlayerDisplayCard.Highlight();
    }
    
    protected virtual void OnPlayerTurnEnded(Player player)
    {
        EnableDiceButton(false);
        
        player.PlayerDisplayCard.RemoveHighlight();
    }
    
    public virtual void OnRollDiceClicked()
    {
        var currentPlayer = GameManager.Instance.CurrentPlayer;
        
        GameUIManager.Instance.OnRollDiceClicked(rollDiceButton);
        currentPlayer.PlayerDisplayCard.SetRolledDiceImage();
        
    }
    
    protected IEnumerator WaitForVisuals()
    {
        yield return new WaitForSeconds(0.5f); // small delay ensures rolled dices being visible
        // GameManager.Instance.setupPhase.PlayerRolledDice();
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
    
}
