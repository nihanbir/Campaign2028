using UnityEngine;

/// <summary>
/// Adapts existing game code to work with the new command/state system.
/// Add this component to your GameManager object.
/// In offline mode: passes commands straight through
/// In online mode: will integrate with networking layer
/// </summary>
[RequireComponent(typeof(GameManager))]
public class NetworkAdapter : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private bool enableCommandSystem = false; // Toggle to test new system
    
    private GameStateManager _stateManager;
    private CommandProcessor _commandProcessor;
    private GameManager _gameManager;
    
    public static NetworkAdapter Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        _gameManager = GetComponent<GameManager>();
    }
    
    private void Start()
    {
        if (enableCommandSystem)
        {
            InitializeCommandSystem();
        }
    }
    
    private void InitializeCommandSystem()
    {
        // Create command processor
        var processorGO = new GameObject("CommandProcessor");
        _commandProcessor = processorGO.AddComponent<CommandProcessor>();
        
        // Create state manager
        _stateManager = new GameStateManager(_gameManager);
        _stateManager.InitializeFromGameManager(_gameManager);
        
        // Link them
        _commandProcessor.Initialize(_stateManager);
        
        Debug.Log("✅ Command system initialized (offline mode)");
        
        // Subscribe to state changes to sync with existing UI
        _stateManager.OnPhaseChanged += OnPhaseChanged;
        _stateManager.OnPlayerRolled += OnPlayerRolled;
        _stateManager.OnPlayerStateChanged += OnPlayerStateChanged;
    }
    
    #region State Change Handlers
    
    private void OnPhaseChanged(GamePhase phase)
    {
        // State already synced to GameManager in GameStateManager.SetPhase
        Debug.Log($"[NetworkAdapter] Phase changed to: {phase}");
    }
    
    private void OnPlayerRolled(int playerId, int roll)
    {
        // TurnFlowBus already raised in GameStateManager
        Debug.Log($"[NetworkAdapter] Player {playerId} rolled {roll}");
    }
    
    private void OnPlayerStateChanged(PlayerState state)
    {
        // Find the corresponding Player object and sync
        var player = _gameManager.players.Find(p => p.playerID == state.PlayerId);
        if (player != null)
        {
            // Player object is already synced in GameStateManager methods
            player.PlayerDisplayCard?.UpdateScore();
        }
    }
    
    #endregion
    
    #region Command Wrappers
    
    /// <summary>
    /// Call this instead of TurnFlowBus.Raise(new TurnEvent(TurnStage.RollDiceRequest)) in Setup Phase
    /// </summary>
    public void RequestSetupRollDice(int playerId)
    {
        if (!enableCommandSystem)
        {
            TurnFlowBus.Instance.Raise(new TurnEvent(TurnStage.RollDiceRequest));
            return;
        }
        
        var command = new SetupRollDiceCommand(playerId);
        _commandProcessor.SubmitCommand(command);
    }
    
    /// <summary>
    /// Call this when an actor card is held/selected
    /// </summary>
    public void RequestSelectActor(int playerId, ActorCard actorCard)
    {
        if (!enableCommandSystem)
        {
            TurnFlowBus.Instance.Raise(new CardInputEvent(CardInputStage.Held, actorCard));
            return;
        }
        
        string actorCardId = actorCard.ToId();
        var command = new SelectActorCommand(playerId, actorCardId);
        _commandProcessor.SubmitCommand(command);
    }
    
    /// <summary>
    /// Call this when a player is clicked to receive an actor
    /// </summary>
    public void RequestConfirmActorAssignment(int playerId, int targetPlayerId)
    {
        if (!enableCommandSystem)
        {
            var targetPlayer = _gameManager.players.Find(p => p.playerID == targetPlayerId);
            TurnFlowBus.Instance.Raise(new CardInputEvent(CardInputStage.Clicked, targetPlayer));
            return;
        }
        
        var command = new ConfirmActorAssignmentCommand(playerId, targetPlayerId);
        _commandProcessor.SubmitCommand(command);
    }

    /// <summary>
    /// Call this instead of TurnFlowBus.Raise(new TurnEvent(TurnStage.RollDiceRequest))
    /// </summary>
    public void RequestRollDice(int playerId)
    {
        if (!enableCommandSystem)
        {
            // Fall back to existing system
            TurnFlowBus.Instance.Raise(new TurnEvent(TurnStage.RollDiceRequest));
            return;
        }
        
        var command = new RollDiceCommand(playerId);
        _commandProcessor.SubmitCommand(command);
    }
    
    /// <summary>
    /// Call this instead of directly assigning actors
    /// </summary>
    public void RequestAssignActor(int playerId, int targetPlayerId, ActorCard actorCard)
    {
        if (!enableCommandSystem)
        {
            // Fall back to existing system
            // Your existing code handles this through bus events
            return;
        }
        
        string actorCardId = actorCard.ToId();
        var command = new AssignActorCommand(playerId, targetPlayerId, actorCardId);
        _commandProcessor.SubmitCommand(command);
    }
    
    /// <summary>
    /// Call this instead of TurnFlowBus.Raise(new MainStageEvent(MainStage.DrawEventCardRequest))
    /// </summary>
    public void RequestDrawEventCard(int playerId)
    {
        if (!enableCommandSystem)
        {
            TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.DrawEventCardRequest));
            return;
        }
        
        var command = new DrawEventCardCommand(playerId);
        _commandProcessor.SubmitCommand(command);
    }
    
    /// <summary>
    /// Call this instead of TurnFlowBus.Raise(new MainStageEvent(MainStage.DrawTargetCardRequest))
    /// </summary>
    public void RequestDrawTargetCard(int playerId)
    {
        if (!enableCommandSystem)
        {
            TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.DrawTargetCardRequest));
            return;
        }
        
        var command = new DrawTargetCardCommand(playerId);
        _commandProcessor.SubmitCommand(command);
    }
    
    /// <summary>
    /// Call this instead of TurnFlowBus.Raise(new MainStageEvent(MainStage.SaveEventCardRequest))
    /// </summary>
    public void RequestSaveEventCard(int playerId, EventCard eventCard)
    {
        if (!enableCommandSystem)
        {
            TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.SaveEventCardRequest, eventCard));
            return;
        }
        
        string cardId = eventCard.ToId();
        var command = new SaveEventCardCommand(playerId, cardId);
        _commandProcessor.SubmitCommand(command);
    }
    
    /// <summary>
    /// Call this instead of TurnFlowBus.Raise(new MainStageEvent(MainStage.ApplyEventCardRequest))
    /// </summary>
    public void RequestApplyEventCard(int playerId, EventCard eventCard)
    {
        if (!enableCommandSystem)
        {
            TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.ApplyEventCardRequest, eventCard));
            return;
        }
        
        string cardId = eventCard.ToId();
        var command = new PlayEventCardCommand(playerId, cardId);
        _commandProcessor.SubmitCommand(command);
    }
    
    #endregion
    
    #region Public API
    
    public bool IsCommandSystemEnabled => enableCommandSystem;
    public GameStateManager StateManager => _stateManager;
    public CommandProcessor CommandProcessor => _commandProcessor;
    
    #endregion
}