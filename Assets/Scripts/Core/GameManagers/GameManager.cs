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

    protected override void Awake()
    {
        base.Awake();
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        InitializePhases();
        SetPhase(mainPhase);
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
    
    // public T GetCurrentPhaseAs<T>() where T : GM_BasePhase
    // {
    //     return _currentPhaseManager as T;
    // }
    
}
public enum GamePhase
{
    None,
    Setup,
    MainGame,
    CivilWar,
    GameOver
}
