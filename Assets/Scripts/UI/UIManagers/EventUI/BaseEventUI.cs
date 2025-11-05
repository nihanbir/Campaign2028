
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseEventUI : MonoBehaviour
{
    [SerializeField] protected GameObject eventScreen;
    [SerializeField] protected Button rollDiceButton;
    
    protected MainPhaseGameManager mainPhase;
    protected MainPhaseUIManager mainUI; 
    protected EventManager eventManager;
    
    protected Player currentPlayer;
    protected int roll;
    
    
    private void Awake()
    {
        eventScreen.SetActive(false);
    }

    public virtual void InitializeEventUI()
    {
        mainPhase = GameManager.Instance?.mainPhase;

        if (mainPhase == null)
        {
            Debug.LogError("MainPhaseGameManager not found. Ensure it's initialized before UI.");
            return;
        }
        mainUI = GameUIManager.Instance.mainUI;
        eventManager = mainPhase.EventManager;
        
        if (rollDiceButton)
            rollDiceButton.onClick.AddListener(OnRollDiceClicked);
    }

    public virtual void OnRollDiceClicked()
    {
        GameUIManager.Instance.OnRollDiceClicked(rollDiceButton);
        currentPlayer.PlayerDisplayCard.SetRolledDiceImage();
        
        roll = GameUIManager.Instance.DiceRoll;
        Debug.Log($"Player rolled: {roll}");
        
    }
    
    protected virtual void ReturnToMainPhaseUI()
    {
        mainUI.gameObject.SetActive(true);
        eventScreen.SetActive(false);
        eventManager.activeEventUI = null;
    }
    
    protected void CreateCardInTransform<T>(GameObject prefab, Transform uiParent, Card cardToSet)
        where T : MonoBehaviour, IDisplayCard
    {
        // Clear previous card(s)
        foreach (Transform child in uiParent)
            Destroy(child.gameObject);

        var cardGO = Instantiate(prefab, uiParent);
        cardGO.SetActive(true);

        if (cardGO.TryGetComponent(out T displayCard))
        {
            displayCard.SetCard(cardToSet);
        }
        else
        {
            Debug.LogError($"{prefab.name} is missing {typeof(T).Name} component.");
        }
        
        if (displayCard.TryGetComponent(out RectTransform rt))
        {
            rt.anchoredPosition = Vector2.zero;
        }
    }
}
