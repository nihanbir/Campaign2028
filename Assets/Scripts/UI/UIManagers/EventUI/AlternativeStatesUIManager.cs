
using UnityEngine;
using UnityEngine.UI;

public class AlternativeStatesUIManager : MonoBehaviour
{
    [SerializeField] private GameObject eventScreen;
    
    [SerializeField] private Transform playerUI;
    [SerializeField] private Transform leftCardUI;
    [SerializeField] private Transform rightCardUI;
    [SerializeField] private Button rollDiceButton;

    private MainPhaseGameManager _mainPhase;
    private MainPhaseUIManager _mainUI; 
    private EventManager _eventManager;

    private Player _currentPlayer;

    private void Awake()
    {
        eventScreen.SetActive(false);
    }
    
    public void InitializeManager()
    {
        _mainPhase = GameManager.Instance?.mainPhase;

        if (_mainPhase == null)
        {
            Debug.LogError("MainPhaseGameManager not found. Ensure it's initialized before UI.");
            return;
        }
        _mainUI = GameUIManager.Instance.mainUI;
        _eventManager = _mainPhase.EventManager;
        _eventManager.OnAltStatesActive += ShowUI;
        _eventManager.OnAltStatesCompleted += ReturnToMainPhaseUI;
        
        if (rollDiceButton)
            rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        
    }

    private void ReturnToMainPhaseUI()
    {
        _mainUI.gameObject.SetActive(true);
        eventScreen.SetActive(false);
    }

    private void ShowUI(Player player, StateCard card1, StateCard card2)
    {
        _currentPlayer = player;
        CreateCardInTransform<PlayerDisplayCard>(player.PlayerDisplayCard.gameObject, playerUI, player.assignedActor);

        if (card1 != null)
        {
            CreateCardInTransform<StateDisplayCard>(_mainUI.stateCardPrefab, leftCardUI, card1);
        }
        
        if (card2 != null)
        {
            CreateCardInTransform<StateDisplayCard>(_mainUI.stateCardPrefab, rightCardUI, card2);
        }
        
        eventScreen.SetActive(true);
        
    }

    private void CreateCardInTransform<T>(GameObject prefab, Transform uiParent, Card cardToSet)
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
    
    public void OnRollDiceClicked()
    {
        GameUIManager.Instance.OnRollDiceClicked(rollDiceButton);
        _currentPlayer.PlayerDisplayCard.SetRolledDiceImage();
        
        int roll = GameUIManager.Instance.DiceRoll;
        Debug.Log($"Player rolled: {roll}");
        _eventManager.EvaluateAltStatesCapture(roll);
    }
}
