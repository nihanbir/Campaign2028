using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

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
            if (displayCard != null)
            {
                displayCard.SetActor(card);
            }
            else
            {
                Debug.LogError("PlayerActorDisplayPrefab missing ActorDisplayCard component.");
            }

            RectTransform rt = uiInstance.GetComponent<RectTransform>();
            if (rt != null)
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
        if (!GameManager.Instance)
        {
            Debug.LogError("GameManager instance is not set");
            return;
        }
        int count = GameManager.Instance.players.Count;
        
        // Calculate total width of all cards including spacing
        float totalWidth = (count - 1) * spacingBetweenPlayerCards;
        
        for (int i = 0; i < count; i++)
        {
            var player = GameManager.Instance.players[i];
            GameObject uiInstance = Instantiate(playerDisplayPrefab, playerUIParent);
            UnassignedPlayerDisplayCard displayCard = uiInstance.GetComponent<UnassignedPlayerDisplayCard>();
            if (displayCard != null)
            {
                displayCard.SetUnassignedPlayerCard(player.playerID.ToString());
            }
            else
            {
                Debug.LogError("ActivePlayerDisplayPrefab missing ActivePlayerDisplayCard component.");
            }

            RectTransform rt = uiInstance.GetComponent<RectTransform>();
            if (rt != null)
            {
                // Position cards so the group is centered
                float xPos = i * spacingBetweenPlayerCards - totalWidth / 2f;
                rt.anchoredPosition = new Vector2(xPos, 0);
            }
        }
    }
    
    public void OnRollDiceClicked()
    {
        GameUIManager.Instance.OnRollDiceClicked(rollDiceButton);
    }
}