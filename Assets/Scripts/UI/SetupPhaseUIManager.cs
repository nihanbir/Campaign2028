using System.Collections;
using System.Collections.Generic;
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
    
    [HideInInspector] public List<UnassignedPlayerDisplayCard> unassignedPlayerCards;
    
    [Header("Actor Card")]
    public GameObject actorDisplayPrefab; // Assign prefab in inspector
    public Transform actorUIParent; // Assign a UI container (e.g., a panel) in inspector
    public float spacingBetweenActorCards = 300;
    
    [Header("Assigned Actor Card")]
    public Transform assignedActorUIParent;
    public float spacingBetweenAssignedCards = 100;
    
    private int assignedActorCardCount;
    private ActorDisplayCard selectedActorCard;
    private CanvasGroup canvasGroup;
    private List<Player> playersToRoll; // players who need to roll dice this round
    private bool rerollActive = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        
        canvasGroup = setupGamephase.GetComponent<CanvasGroup>();

        CreateUnassignedPlayerUI();
        
        // Initially all players roll
        playersToRoll = new List<Player>(GameManager.Instance.players);
        
        GameManager.Instance.StartTurn();

    }

    private void EnableCanvasGroup(bool enable)
    {
        canvasGroup.interactable = enable;
        canvasGroup.blocksRaycasts = enable;
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
                // unassignedActorCards.Add(displayCard);
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
                unassignedPlayerCards.Add(displayCard);
                // unassignedPlayers.Add(player);
            }
            else
            {
                Debug.LogError("PlayerDisplayPrefab missing PlayerDisplayCard component.");
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
        
        var currentPlayer = GameManager.Instance.CurrentPlayer;
        
        GameUIManager.Instance.OnRollDiceClicked(rollDiceButton);
        
        var currentPlayerCard = GetUnassignedPlayerCardForPlayer(currentPlayer);
        currentPlayerCard.SetRolledDice(GameUIManager.Instance._diceRoll);
        
        // Remove player from playersToRoll since they rolled
        playersToRoll.Remove(currentPlayer);
        
        if (playersToRoll.Count == 0)
        {
            // All players rolled, check for ties
            rerollActive = CheckForDiceRollTies();
        }
        
        GameManager.Instance.EndTurn();
    }

    private bool CheckForDiceRollTies()
    {
        // Group players by diceRoll
        Dictionary<int, List<Player>> rollGroups = new Dictionary<int, List<Player>>();
        
        foreach (var card in unassignedPlayerCards)
        {
            int roll = card.diceRoll;

            if (!rollGroups.ContainsKey(roll))
                rollGroups[roll] = new List<Player>();

            rollGroups[roll].Add(card.player);
        }

        // Find all tie groups (rolls with more than 1 player)
        foreach (var group in rollGroups)
        {
            if (group.Value.Count > 1)
            {
                playersToRoll.AddRange(group.Value);
            }
        }

        if (playersToRoll.Count > 0)
        {
            Debug.Log($"Ties detected for dice rolls. Players tied: {string.Join(", ", playersToRoll.ConvertAll(p => p.playerID.ToString()))}. They will reroll.");
            return true;
        }
        
        Debug.Log("No ties detected. Proceeding with game.");
        return false;
    }

    public void OnPlayerTurnStarted(Player currentPlayer)
    {
        if (rerollActive)
        {
            int index = 0;
            foreach (var card in unassignedPlayerCards)
            {
                if (playersToRoll.Contains(card.player))
                {
                    card.diceRoll = 0;
                    card.diceImage.gameObject.SetActive(false);
                }
                else
                {
                    // Optionally disable or hide dice for players not rerolling
                    card.diceImage.gameObject.SetActive(true);
                }
            }
            
            // Set current player to tied player
            GameManager.Instance.currentPlayerIndex = GameManager.Instance.players.IndexOf(playersToRoll[index]);
            currentPlayer = playersToRoll[index];
            index++;
        }
        
        if (AIManager.Instance.IsAIPlayer(currentPlayer))
        {
            var aiPlayer = AIManager.Instance.GetAIPlayer(currentPlayer);
            StartCoroutine(AITurnCoroutine(aiPlayer));
            EnableCanvasGroup(false);
        }
        else
        {
            var playerCard = GetUnassignedPlayerCardForPlayer(currentPlayer);
            if (playerCard && playerCard.diceRoll == 0)
            {
                EnableCanvasGroup(true);
            }
        }
        // Optionally, highlight current player's UI card
    }
    
    private IEnumerator AITurnCoroutine(Player aiPlayer)
    {
        // Wait a short delay to simulate thinking
        yield return new WaitForSeconds(1f);

        var playerCard = GetUnassignedPlayerCardForPlayer(aiPlayer);
        if (playerCard && playerCard.diceRoll == 0)
        {
            OnRollDiceClicked();
        }
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
        
        if (player != GameManager.Instance.CurrentPlayer)
        {
            Debug.LogWarning("It's not this player's turn.");
            return;
        }
        
        ActorCard actorToAssign = selectedActorCard.GetActorCard();
        
        if (player)
        {
            player.assignedActor = actorToAssign;
            // unassignedPlayers.Remove(player);
            Debug.Log($"Assigned actor {actorToAssign.cardName} to player {player.playerID}");
            
            // playerCard.gameObject.SetActive(false);
            Destroy(playerCard);
            
            assignedActorCardCount++;
            
            // Optionally remove or disable the assigned actor card so it can't be assigned again
            RemoveAssignedActorCard(selectedActorCard);
            
            UpdateAssignedActorUI(player);
        
            // Clear selection
            selectedActorCard = null;
            
            GameManager.Instance.EndTurn();
        }
        else
        {
            Debug.LogError($"Player with ID {player.playerID} not found.");
        }
    }

    public UnassignedPlayerDisplayCard GetUnassignedPlayerCardForPlayer(Player player)
    {
        foreach (var card in unassignedPlayerCards)
        {
            if (card.player == player)
            {
                return card;
            }
        }
        return null;
    }
    
    private void RemoveAssignedActorCard(ActorDisplayCard actorCard)
    {
        // Disable or destroy the actor card UI so it can't be assigned again
        // actorCard.gameObject.SetActive(false);
        Destroy(actorCard);
        
    }
}