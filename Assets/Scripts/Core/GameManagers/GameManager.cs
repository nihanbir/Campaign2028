using System;
using UnityEngine;

public class GameManager : GameManagerBase
{
    public static GameManager Instance { get; private set; }
    
    [HideInInspector] public GM_SetupPhase setupPhase;
    [HideInInspector] public GM_MainPhase mainPhase;

    private GM_BasePhase _currentPhaseManager;

    public GamePhase CurrentPhase => _currentPhaseManager.PhaseType;

    public event Action<GM_BasePhase> OnPhaseChanged;

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
        SetPhase(setupPhase);
    }
    
    public void SetPhase(GM_BasePhase newPhase)
    {
        if (_currentPhaseManager == newPhase) return;

        Debug.Log($"=== Transitioning to {newPhase.GetType().Name} ===");

        _currentPhaseManager = newPhase;
        OnPhaseChanged?.Invoke(newPhase);
    }
    
    private void InitializePhases()
    {
        setupPhase = new GM_SetupPhase();
        mainPhase = new GM_MainPhase();
    }
    
    public T GetCurrentPhaseAs<T>() where T : GM_BasePhase
    {
        return _currentPhaseManager as T;
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
