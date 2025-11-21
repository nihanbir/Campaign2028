using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    public static AIManager Instance;

    [Header("AI Configuration")]
    [SerializeField] private int numberOfAIPlayers = 5;
    [SerializeField] private GameObject aiPlayerPrefab;

    [HideInInspector] public List<AIPlayer> aiPlayers = new List<AIPlayer>();

    // Phase managers (plain C# classes)
    public AM_SetupPhase setupAI;
    public AM_MainPhase mainAI;

    [HideInInspector] public GameManager game;
    
    private IAIPhase _activePhase;

    public void ActivatePhase(IAIPhase phase)
    {
        _activePhase?.OnExit();
        _activePhase = phase;
        _activePhase.OnEnter();
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        game = GameManager.Instance;
        
        //TODO: have one active at a time
        setupAI = new AM_SetupPhase(this);
        mainAI = new AM_MainPhase(this);
        
    }

    public bool IsAIPlayer(Player player) => player is AIPlayer;

    public AIPlayer GetAIPlayer(Player player) => player as AIPlayer;
    
    public List<AIPlayer> GetAllAIPlayers()
    {
        return game.players.OfType<AIPlayer>().ToList();
    }
}