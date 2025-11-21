using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Processes commands in offline or multiplayer mode.
/// In offline: validates and executes immediately
/// In multiplayer: sends to server, waits for confirmation
/// </summary>
public class CommandProcessor : MonoBehaviour
{
    public static CommandProcessor Instance { get; private set; }
    
    [Header("Mode")]
    [SerializeField] private bool isOnlineMode = false;
    
    private GameStateManager _stateManager;
    private Queue<GameCommand> _pendingCommands = new();
    
    // Events
    public event Action<GameCommand> OnCommandExecuted;
    public event Action<GameCommand, string> OnCommandFailed;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void Initialize(GameStateManager stateManager)
    {
        _stateManager = stateManager;
    }
    
    /// <summary>
    /// Submit a command for execution
    /// </summary>
    public void SubmitCommand(GameCommand command)
    {
        if (isOnlineMode)
        {
            // In online mode: send to server
            SendCommandToServer(command);
        }
        else
        {
            // In offline mode: execute immediately
            ExecuteCommandLocal(command);
        }
    }
    
    /// <summary>
    /// Execute command locally (offline mode or server)
    /// </summary>
    private void ExecuteCommandLocal(GameCommand command)
    {
        if (_stateManager == null)
        {
            Debug.LogError("GameStateManager not initialized!");
            OnCommandFailed?.Invoke(command, "State manager not initialized");
            return;
        }
        
        bool success = _stateManager.ExecuteCommand(command);
        
        if (success)
        {
            OnCommandExecuted?.Invoke(command);
            Debug.Log($"Command executed: {command.GetType().Name}");
        }
        else
        {
            OnCommandFailed?.Invoke(command, "Validation failed");
            Debug.LogWarning($"Command failed: {command.GetType().Name}");
        }
    }
    
    /// <summary>
    /// Send command to server (online mode only)
    /// </summary>
    private void SendCommandToServer(GameCommand command)
    {
        // TODO: Implement when adding networking
        // Example:
        // byte[] data = command.Serialize();
        // NetworkManager.SendToServer(data);
        
        Debug.Log($"[Network] Sending command to server: {command.GetType().Name}");
        
        // Add to pending queue
        _pendingCommands.Enqueue(command);
    }
    
    /// <summary>
    /// Receive command result from server (online mode only)
    /// </summary>
    public void OnServerCommandResult(GameCommand command, bool success, string errorMessage = null)
    {
        // Remove from pending
        if (_pendingCommands.Count > 0)
            _pendingCommands.Dequeue();
        
        if (success)
        {
            // Server validated and executed, update local state
            OnCommandExecuted?.Invoke(command);
        }
        else
        {
            OnCommandFailed?.Invoke(command, errorMessage ?? "Server rejected command");
        }
    }
    
    /// <summary>
    /// Receive state update from server (online mode only)
    /// </summary>
    public void OnServerStateUpdate(byte[] stateData)
    {
        if (_stateManager != null)
        {
            _stateManager.DeserializeState(stateData);
        }
    }
    
    public void SetOnlineMode(bool online)
    {
        isOnlineMode = online;
    }
    
    public bool IsOnlineMode => isOnlineMode;
}

/// <summary>
/// Helper extensions to make command submission easier
/// </summary>
public static class CommandProcessorExtensions
{
    public static void Submit(this GameCommand command)
    {
        CommandProcessor.Instance?.SubmitCommand(command);
    }
}