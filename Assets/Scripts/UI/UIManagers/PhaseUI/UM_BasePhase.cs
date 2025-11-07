
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public abstract class UM_BasePhase : MonoBehaviour
{
    protected GameManager game;
    
    [Header("Phase Elements")]     
    public Button rollDiceButton;
    public abstract GamePhase PhaseType { get; }

    protected bool isPlayerAI;
    
    private void Awake()
    {
        game = GameManager.Instance;
        
        game.OnPhaseChanged += OnPhaseChanged;
        
    }
    private void OnDestroy()
    {
        UnsubscribeToPhaseEvents();
        if (game != null)
            game.OnPhaseChanged -= OnPhaseChanged;
    }
    
    private void OnPhaseChanged(GamePhase newPhase)
    {
        if (PhaseType != newPhase)
            OnPhaseDisabled();
        else
            OnPhaseEnabled();
    }

    protected virtual void OnPhaseEnabled()
    {
        rollDiceButton.onClick.RemoveAllListeners();
        rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        
        EnableDiceButton(false);
        
        SubscribeToPhaseEvents();
        
        gameObject.SetActive(true);
    }

    protected virtual void OnPhaseDisabled()
    {
        UnsubscribeToPhaseEvents();
        gameObject.SetActive(false);
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
