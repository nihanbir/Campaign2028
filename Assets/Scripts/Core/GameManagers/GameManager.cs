using UnityEngine;

public class GameManager : GameManagerBase
{
    public static GameManager Instance { get; private set; }

    [HideInInspector] public SetupPhaseGameManager setupPhase;
    [HideInInspector] public MainPhaseGameManager mainPhase;
    
    private GamePhase _currentPhase = GamePhase.CivilWar;
    public GamePhase CurrentPhase
    {
        get => _currentPhase;
        set
        {
            if (_currentPhase != value)
            {
                _currentPhase = value;
                TransitionToPhase(value);
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        LoadDecks();
        InitializePhases();
        CurrentPhase = GamePhase.Setup;
    }
    
    private void InitializePhases()
    {
        setupPhase = new SetupPhaseGameManager(this);
        mainPhase = new MainPhaseGameManager(this);
    }
    
    private void TransitionToPhase(GamePhase newPhase)
    {
        Debug.Log($"=== Transitioning to {newPhase} stage ===");
        
        GameUIManager.Instance.OnTransitionToPhase(newPhase);
        
        switch (newPhase)
        {
            case GamePhase.Setup:
                BeginSetupPhase();
                break;
            
            case GamePhase.MainGame:
                BeginMainGamePhase();
                break;
            
            case GamePhase.CivilWar:
                break;
            
            case GamePhase.GameOver:
                break;
        }
        
    }
    
    private void BeginSetupPhase()
    {
        Debug.Log("Begin setup phase");
        setupPhase.InitializePhase();
    }
    
    private void BeginMainGamePhase()
    {
        Debug.Log("Begin main phase");
        mainPhase.InitializePhase();
    }
}

public enum GamePhase
{
    Setup,
    MainGame,
    CivilWar,
    GameOver
}
