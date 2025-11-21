using System;
using UnityEngine;

public class GameManager : GameManagerBase
{
    public static GameManager Instance { get; private set; }
    
    [HideInInspector] public GM_SetupPhase setupPhase;
    [HideInInspector] public GM_MainPhase mainPhase;

    private GM_BasePhase _currentPhaseManager;
    
    public GamePhase CurrentPhase =>  _currentPhaseManager?.PhaseType ?? GamePhase.None;

    public event Action<GM_BasePhase> OnPhaseChanged;
    
    public static bool IsServer { get; private set; } = true;
    
    protected override void Awake()
    {
        if (!IsServer)
        {
            Destroy(gameObject);
            return;
        }
        
        if (Instance == null) 
            Instance = this;
        
        else Destroy(gameObject);
        
        base.Awake();
    }

    private void Start()
    {
        if (!IsServer) return;
        
        InitializePhases();
        SetPhase(setupPhase);
    }
    
    public void SetPhase(GM_BasePhase newPhase)
    {
        if (CurrentPhase == newPhase.PhaseType) return;

        Debug.Log($"=== Transitioning to {newPhase.GetType().Name} ===");

        _currentPhaseManager = newPhase;
        
        OnPhaseChanged?.Invoke(newPhase);
    }
    
    private void InitializePhases()
    {
        setupPhase = new GM_SetupPhase();
        mainPhase = new GM_MainPhase();
    }
    
    public static void SetNetworkMode(bool isServer)
    {
        IsServer = isServer;
    }
}

public enum GamePhase
{
    None,
    Setup,
    MainGame,
    CivilWar,
    GameOver
}