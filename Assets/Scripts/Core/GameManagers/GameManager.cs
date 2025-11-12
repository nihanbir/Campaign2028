using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager : GameManagerBase
{
    public static GameManager Instance { get; private set; }
    
    [HideInInspector] public GM_SetupPhase setupPhase;
    [HideInInspector] public GM_MainPhase mainPhase;

    private GM_BasePhase _currentPhaseManager;
    
    public GamePhase CurrentPhase =>  _currentPhaseManager?.PhaseType ?? GamePhase.None;

    public event Action<GM_BasePhase> OnPhaseChanged;
    
    public int DiceRoll { get; private set; }

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
    
    // In GameManager
    public void HandleRollRequest(Player player)
    {
        if (_currentPhaseManager == null) return;
        
        DiceRoll = Random.Range(1, 7);
        
        //TODO: make this generic instead of event stage
        EventCardBus.Instance.Raise(new CardEvent(EventStage.PlayerRolled, new PlayerRolledData(player, DiceRoll)));
        
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



// public sealed class GamePhaseBus
// {
//     private static GamePhaseBus _instance;
//     public static GamePhaseBus Instance => _instance ??= new GamePhaseBus();
//
//     public event Action<GameEvent> OnEvent;
//
//     public void Raise(GameEvent e)
//     {
// #if UNITY_EDITOR
//         Debug.Log($"[EventBus] {e.stage}");
// #endif
//         OnEvent?.Invoke(e);
//     }
//
//     public void Clear() => OnEvent = null;
// }
//
// public readonly struct GameEvent
// {
//     public readonly EventStage stage;
//     public readonly object Payload; // keep generic for flexibility
//
//     public GameEvent(EventStage stage, object payload)
//     {
//         this.stage = stage;
//         Payload = payload;
//     }
// }

public enum PhaseStage
{
    None,
    RollDiceRequest,
    PlayerRolled,           // Player rolled value (for UI dice feedback)
    ClientAnimationCompleted,

}