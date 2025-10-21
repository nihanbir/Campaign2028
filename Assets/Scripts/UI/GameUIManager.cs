using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance;
    
    [Header("Player")]
    public GameObject playerActorDisplayPrefab; // Assign prefab in inspector
    public Transform playerUIParent; // Assign a UI container (e.g., a panel) in inspector
    public float spacingBetweenActorCards = 300;
    
    [Header("Dice & Actions")]
    public Button rollDiceButton;
    public Sprite[] diceFaces; // 6 sprites for dice faces 1-6
    private int initiativeRoll;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        CreatePlayerUI();
    }

    #region Dice & Actions
    public void OnRollDiceClicked()
    {
        initiativeRoll = Random.Range(1, 7);
        switch (initiativeRoll)
        {
            case 1:
                
                rollDiceButton.image.sprite = diceFaces[0];
                return;
            case 2:
                rollDiceButton.image.sprite = diceFaces[1];
                return;
            case 3:
                rollDiceButton.image.sprite = diceFaces[2];
                return;
            case 4:
                rollDiceButton.image.sprite = diceFaces[3];
                return;
            case 5:
                rollDiceButton.image.sprite = diceFaces[4];
                return;
            case 6:
                rollDiceButton.image.sprite = diceFaces[5];
                return;
        }
    }
    #endregion Dice & Actions
    #region PlayersAndActorCards
    void CreatePlayerUI()
    {
        int index = 0;
        
        int count = GameManager.Instance.players.Count;
        
        // Calculate total width of all cards including spacing
        float totalWidth = (count - 1) * spacingBetweenActorCards;
        
        for (int i = 0; i < count; i++)
        {
            var player = GameManager.Instance.players[i];
            GameObject uiInstance = Instantiate(playerActorDisplayPrefab, playerUIParent);
            ActorDisplayCard displayCard = uiInstance.GetComponent<ActorDisplayCard>();
            if (displayCard != null)
            {
                displayCard.SetActor(player.assignedActor);
            }
            else
            {
                Debug.LogError("PlayerActorDisplayPrefab missing ActorDisplayCard component.");
            }

            RectTransform rt = uiInstance.GetComponent<RectTransform>();
            if (rt != null)
            {
                // Position cards so the group is centered
                float xPos = i * spacingBetweenActorCards - totalWidth / 2f;
                rt.anchoredPosition = new Vector2(xPos, 0);
            }
        }
    }
    #endregion PlayersAndActorCards
    
}
