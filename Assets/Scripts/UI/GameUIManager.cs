using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance;
    
    [Header("Player")]
    public GameObject playerActorDisplayPrefab; // Assign prefab in inspector
    public Transform playerUIParent; // Assign a UI container (e.g., a panel) in inspector
    public float spacingBetweenActorCards = 300;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        CreatePlayerUI();
    }

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
}
