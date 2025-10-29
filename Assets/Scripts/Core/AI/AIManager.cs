using System.Collections.Generic;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    public static AIManager Instance;

    [Header("AI Configuration")]
    [SerializeField] private int numberOfAIPlayers = 5;
    [SerializeField] private GameObject aiPlayerPrefab;

    [HideInInspector] public List<AIPlayer> aiPlayers = new List<AIPlayer>();

    // Phase managers (plain C# classes)
    public SetupPhaseAIManager setupAI;
    public MainPhaseAIManager mainAI;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        setupAI = new SetupPhaseAIManager(this);
        mainAI = new MainPhaseAIManager(this);
    }

    public void CreateAIPlayers(int humanPlayerCount)
    {
        int aiCount = numberOfAIPlayers - humanPlayerCount;
        aiCount = Mathf.Clamp(aiCount, 1, 5);

        for (int i = 0; i < aiCount; i++)
        {
            CreateAIPlayer(i);
        }

        Debug.Log($"Created {aiCount} AI players");
    }

    private void CreateAIPlayer(int index)
    {
        GameObject aiObj = aiPlayerPrefab
            ? Instantiate(aiPlayerPrefab, transform)
            : new GameObject($"AI_Player_{index}");

        var ai = aiObj.GetComponent<AIPlayer>() ?? aiObj.AddComponent<AIPlayer>();
        ai.playerID = 100 + index;

        aiPlayers.Add(ai);

        if (GameManager.Instance != null)
            GameManager.Instance.players.Add(ai);
    }

    public bool IsAIPlayer(Player player) => player is AIPlayer;

    public AIPlayer GetAIPlayer(Player player) => player as AIPlayer;
}