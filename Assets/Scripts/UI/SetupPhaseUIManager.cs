using UnityEngine;
using UnityEngine.UI;

public class SetupPhaseUIManager : MonoBehaviour
{
    public static SetupPhaseUIManager Instance;
    
    [Header("Setup Phase")]     
    public GameObject setupGamephase;
    public Button rollDiceButton;
    
    [Header("Player")]
    public GameObject playerDisplayPrefab;   // Assign in inspector: prefab for player UI element
    public Transform playerUIParent; // Assign in inspector: parent transform for player UI elements
    public float spacingBetweenPlayerCards = 150;
    
    [Header("Actor Card")]
    public GameObject actorDisplayPrefab; // Assign prefab in inspector
    public Transform actorUIParent; // Assign a UI container (e.g., a panel) in inspector
    public float spacingBetweenActorCards = 300;
    
    [Header("Assigned Actor Card")]
    public Transform assignedActorUIParent;
    public float spacingBetweenAssignedCards = 100;

    private int assignedActorCardCount;
    private ActorDisplayCard selectedActorCard;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        
        CreateUnassignedPlayerUI();
        CreateActorCardUI();

    }

    void CreateActorCardUI()
    {
        int index = 0;
        
        int count = GameManager.Instance.gameDeckData.GetActorDeck().Count;
        
        // Calculate total width of all cards including spacing
        float totalWidth = (count - 1) * spacingBetweenActorCards;

        foreach (var card in GameManager.Instance.gameDeckData.GetActorDeck())
        {
            GameObject uiInstance = Instantiate(actorDisplayPrefab, actorUIParent);
            ActorDisplayCard displayCard = uiInstance.GetComponent<ActorDisplayCard>();
            if (displayCard)
            {
                displayCard.SetActor(card);
            }
            else
            {
                Debug.LogError("PlayerActorDisplayPrefab missing ActorDisplayCard component.");
            }

            RectTransform rt = uiInstance.GetComponent<RectTransform>();
            if (rt)
            {
                // Position cards so the group is centered
                float xPos = index * spacingBetweenActorCards - totalWidth / 2f;
                rt.anchoredPosition = new Vector2(xPos, 0);
            }
            index++;
        }
    }
    
    void CreateUnassignedPlayerUI()
    {
        int index = 0;
        if (!GameManager.Instance)
        {
            Debug.LogError("GameManager instance is not set");
            return;
        }
        int count = GameManager.Instance.players.Count;
        
        // Calculate total width of all cards including spacing
        float totalWidth = (count - 1) * spacingBetweenPlayerCards;

        foreach (var player in GameManager.Instance.players)
        {
            GameObject uiInstance = Instantiate(playerDisplayPrefab, playerUIParent);
            UnassignedPlayerDisplayCard displayCard = uiInstance.GetComponent<UnassignedPlayerDisplayCard>();
            if (displayCard)
            {
                displayCard.SetUnassignedPlayerCard(player);
            }
            else
            {
                Debug.LogError("ActivePlayerDisplayPrefab missing ActivePlayerDisplayCard component.");
            }

            RectTransform rt = uiInstance.GetComponent<RectTransform>();
            if (rt)
            {
                // Position cards so the group is centered
                float xPos = index * spacingBetweenPlayerCards - totalWidth / 2f;
                rt.anchoredPosition = new Vector2(xPos, 0);
            }

            index++;
        }
    }
    
    void UpdateAssignedActorUI(Player player)
    {
        GameObject uiInstance = Instantiate(actorDisplayPrefab, assignedActorUIParent);
        ActorDisplayCard displayCard = uiInstance.GetComponent<ActorDisplayCard>();
        if (displayCard)
        {
            displayCard.SetActor(player.assignedActor);
            
            int count = GameManager.Instance.players.Count;
            // Calculate total width of all cards including spacing
            float totalWidth = (count - 1) * spacingBetweenPlayerCards;
            
            RectTransform rt = uiInstance.GetComponent<RectTransform>();
            if (rt)
            {
                float xPos = assignedActorCardCount * spacingBetweenAssignedCards - totalWidth / 2;

                rt.anchoredPosition = new Vector2(xPos, 0);
            }
        }
        else
        {
            Debug.LogError("PlayerActorDisplayPrefab missing ActorDisplayCard component.");
        }
    }

    
    public void OnRollDiceClicked()
    {
        GameUIManager.Instance.OnRollDiceClicked(rollDiceButton);
    }
    
    public void SelectActorCard(ActorDisplayCard actorCard)
    {
        selectedActorCard = actorCard;
        Debug.Log($"Selected actor: {actorCard.GetActorCard().cardName}");
        // Optionally update UI to highlight selected actor card
    }
    
    public void AssignSelectedActorToPlayer(Player player, UnassignedPlayerDisplayCard playerCard)
    {
        if (selectedActorCard)
        {
            Debug.LogWarning("No actor card selected to assign.");
            return;
        }
        
        ActorCard actorToAssign = selectedActorCard.GetActorCard();
        
        if (player)
        {
            player.assignedActor = actorToAssign;
            Debug.Log($"Assigned actor {actorToAssign.cardName} to player {player.playerID}");
            
            playerCard.gameObject.SetActive(false);
            
            assignedActorCardCount++;
            
            // Optionally remove or disable the assigned actor card so it can't be assigned again
            RemoveAssignedActorCard(selectedActorCard);
            
            UpdateAssignedActorUI(player);
        
            // Clear selection
            selectedActorCard = null;
        }
        else
        {
            Debug.LogError($"Player with ID {player.playerID} not found.");
        }
    }

    private void RemoveAssignedActorCard(ActorDisplayCard actorCard)
    {
        // Disable or destroy the actor card UI so it can't be assigned again
        actorCard.gameObject.SetActive(false);
        
    }
}