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
        
        TurnFlowBus.Instance.OnEvent += OnTurnEvent;
        
    }
    
    private void OnTurnEvent(IGameEvent e)
    {
        if (e is TurnEvent t)
        {
            if (t.stage == TurnStage.RollDiceRequest)
            {
                var data = (PlayerRolledData)t.Payload;
                OnPlayerRequestedRoll(CurrentPlayer);
            }
        }
    }

    private void Start()
    {
        InitializePhases();
        SetPhase(setupPhase);
    }
    
    public void SetPhase(GM_BasePhase newPhase)
    {
        if (CurrentPhase == newPhase.PhaseType) return;

        Debug.Log($"=== Transitioning to {newPhase.GetType().Name} ===");

        _currentPhaseManager = newPhase;
        
        //TODO:make this a bus later
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
    
    private void OnPlayerRequestedRoll(Player player)
    {
        if (_currentPhaseManager == null) return;
        
        DiceRoll = Random.Range(1, 7);
        
        TurnFlowBus.Instance.Raise(new TurnEvent(TurnStage.PlayerRolled, new PlayerRolledData(player, DiceRoll)));
        
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

public sealed class TurnFlowBus
{
    private static TurnFlowBus _instance;
    public static TurnFlowBus Instance => _instance ??= new TurnFlowBus();

    public event Action<IGameEvent> OnEvent;
    public void Raise(IGameEvent e)
    {
#if UNITY_EDITOR
        Debug.Log($"[EventBus] {e}");
#endif
        OnEvent?.Invoke(e);
    }

    public void Clear() => OnEvent = null;
}

public readonly struct TurnEvent : IGameEvent
{
    public readonly TurnStage stage;
    public readonly object Payload; // keep generic for flexibility

    public TurnEvent(TurnStage stage, object payload)
    {
        this.stage = stage;
        Payload = payload;
    }
}

public enum TurnStage
{
    None,
    PlayerTurnStarted,
    PlayerTurnEnded,
    RollDiceRequest,
    PlayerRolled,
    AnimationCompleted

}

public sealed class PlayerRolledData
{
    public Player Player;
    public int    Roll;
    public PlayerRolledData(Player p, int r) { Player = p; Roll = r; }
}

public sealed class RollDiceRequest
{
}

public sealed class PlayerTurnStartedData
{
    public Player Player;
    public PlayerTurnStartedData(Player p) { Player = p;}
}

public sealed class PlayerTurnEndedData
{
    public Player Player;
    public PlayerTurnEndedData(Player p) { Player = p;}
}