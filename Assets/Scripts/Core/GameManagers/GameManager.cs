using System;
using UnityEngine;

public class GameManager : GameManagerBase
{
    public static GameManager Instance { get; private set; }
    
    [HideInInspector] public GM_SetupPhase setupPhase;
    [HideInInspector] public GM_MainPhase mainPhase;

    private GamePhase _currentPhase = GamePhase.None;
    
    public GamePhase CurrentPhase => _currentPhase;

    public event Action<GamePhase> OnPhaseChanged;

    protected override void Awake()
    {
        base.Awake();
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        InitializePhases();
        SetPhase(GamePhase.Setup);
    }
    
    public void SetPhase(GamePhase newPhase)
    {
        if (CurrentPhase == newPhase) return;

        Debug.Log($"=== Transitioning to {newPhase.GetType().Name} ===");

        _currentPhase = newPhase;
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
