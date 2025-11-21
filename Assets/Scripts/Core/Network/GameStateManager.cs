using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Authoritative game state manager. All game state changes go through this class.
/// In multiplayer: server runs this, clients mirror the state.
/// In offline: this runs locally and updates existing GameManager.
/// </summary>
public class GameStateManager
{
    // Core state
    public GamePhase CurrentPhase { get; private set; }
    public int CurrentPlayerId { get; private set; }
    public int CurrentPlayerIndex { get; private set; }
    
    // Players
    private readonly Dictionary<int, PlayerState> _playerStates = new();
    public IReadOnlyDictionary<int, PlayerState> PlayerStates => _playerStates;
    
    // Decks (stored as card IDs)
    private List<string> _stateDeck = new();
    private List<string> _institutionDeck = new();
    private List<string> _eventDeck = new();
    private List<string> _actorDeck = new();
    
    // Current cards in play
    public string CurrentTargetCardId { get; private set; }
    public string CurrentEventCardId { get; private set; }
    
    // Card ownership tracking
    private readonly Dictionary<string, int> _cardOwners = new(); // cardId -> playerId
    
    // Setup phase state
    private readonly Dictionary<int, int> _setupRolls = new();
    private List<int> _playersToRoll = new();
    
    // Events for state changes
    public event Action<GamePhase> OnPhaseChanged;
    public event Action<int> OnCurrentPlayerChanged;
    public event Action<PlayerState> OnPlayerStateChanged;
    public event Action<string, int> OnCardOwnerChanged; // cardId, newOwnerId
    public event Action<int, int> OnPlayerRolled; // playerId, roll
    public event Action<string> OnTargetCardDrawn;
    public event Action<string> OnEventCardDrawn;
    
    // Reference to existing GameManager (for offline mode compatibility)
    private GameManager _gameManager;
    
    public GameStateManager(GameManager gameManager = null)
    {
        _gameManager = gameManager;
    }
    
    /// <summary>
    /// Initialize game state from existing GameManager (offline mode)
    /// </summary>
    public void InitializeFromGameManager(GameManager gm)
    {
        _gameManager = gm;
        
        // Initialize card registry
        CardRegistry.Instance.InitializeFromGameDeck(gm.gameDeckData);
        
        // Convert decks to IDs
        _stateDeck = gm.stateDeck.ToIds();
        _institutionDeck = gm.institutionDeck.ToIds();
        _eventDeck = gm.eventDeck.ToIds();
        _actorDeck = gm.actorDeck.ToIds();
        
        // Initialize player states
        for (int i = 0; i < gm.players.Count; i++)
        {
            var player = gm.players[i];
            var isAI = player is AIPlayer;
            var playerState = new PlayerState(player.playerID, isAI);
            _playerStates[player.playerID] = playerState;
        }
        
        CurrentPhase = GamePhase.None;
        CurrentPlayerIndex = 0;
        UpdateCurrentPlayer();
    }
    
    /// <summary>
    /// Execute a validated command
    /// </summary>
    public bool ExecuteCommand(GameCommand command)
    {
        if (!command.Validate(this))
        {
            Debug.LogWarning($"Command validation failed: {command.GetType().Name}");
            return false;
        }
        
        command.Execute(this);
        return true;
    }
    
    #region State Queries
    
    public PlayerState GetPlayerState(int playerId)
    {
        return _playerStates.GetValueOrDefault(playerId);
    }
    
    public bool IsActorAvailable(string actorCardId)
    {
        return _actorDeck.Contains(actorCardId);
    }
    
    public bool HasEventCardsRemaining()
    {
        return _eventDeck.Count > 0;
    }
    
    public bool HasTargetCardsRemaining()
    {
        return _stateDeck.Count + _institutionDeck.Count > 0;
    }
    
    public EventCard GetEventCard(string cardId)
    {
        return CardRegistry.Instance.GetEventCard(cardId);
    }
    
    public int GetCardOwner(string cardId)
    {
        return _cardOwners.GetValueOrDefault(cardId, -1);
    }
    
    #endregion
    
    #region State Mutations
    
    public void SetPhase(GamePhase newPhase)
    {
        if (CurrentPhase == newPhase) return;
        
        CurrentPhase = newPhase;
        OnPhaseChanged?.Invoke(newPhase);
        
        // Sync to existing GameManager
        if (_gameManager != null)
        {
            var phaseManager = newPhase switch
            {
                GamePhase.Setup => (GM_BasePhase)_gameManager.setupPhase,
                GamePhase.MainGame => _gameManager.mainPhase,
                _ => null
            };
            
            if (phaseManager != null)
                _gameManager.SetPhase(phaseManager);
        }
    }
    
    public void ProcessDiceRoll(int playerId, int roll)
    {
        var playerState = GetPlayerState(playerId);
        if (playerState == null) return;
        
        playerState.LastRoll = roll;
        playerState.ConsumeRoll();
        
        OnPlayerRolled?.Invoke(playerId, roll);
        OnPlayerStateChanged?.Invoke(playerState);
        
        // Raise bus event for existing code
        TurnFlowBus.Instance.Raise(new TurnEvent(TurnStage.PlayerRolled, new PlayerRolledData(GetPlayer(playerId), roll)));
    }
    
    public void AssignActor(int playerId, string actorCardId)
    {
        var playerState = GetPlayerState(playerId);
        if (playerState == null) return;
        
        playerState.AssignedActorCardId = actorCardId;
        _actorDeck.Remove(actorCardId);
        
        OnPlayerStateChanged?.Invoke(playerState);
        
        // Sync to existing Player object
        var player = GetPlayer(playerId);
        if (player != null)
        {
            var actorCard = CardRegistry.Instance.GetActorCard(actorCardId);
            player.assignedActor = actorCard;
            
            // Raise bus event
            TurnFlowBus.Instance.Raise(new SetupStageEvent(SetupStage.ActorAssigned, new ActorAssignedData(player, actorCard)));
        }
    }
    
    public void CaptureCard(int playerId, string cardId)
    {
        var playerState = GetPlayerState(playerId);
        if (playerState == null) return;
        
        var card = CardRegistry.Instance.GetCard(cardId);
        if (card == null) return;
        
        // Update ownership
        _cardOwners[cardId] = playerId;
        
        // Update player state
        switch (card)
        {
            case StateCard state:
                playerState.AddStateCard(cardId, state.electoralVotes);
                _stateDeck.Remove(cardId);
                break;
            case InstitutionCard inst:
                playerState.AddInstitutionCard(cardId);
                _institutionDeck.Remove(cardId);
                break;
        }
        
        OnCardOwnerChanged?.Invoke(cardId, playerId);
        OnPlayerStateChanged?.Invoke(playerState);
        
        // Sync to existing Player object
        var player = GetPlayer(playerId);
        if (player != null)
        {
            player.CaptureCard(card);
            TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.CardCaptured, new CardCapturedData(player, card)));
        }
    }
    
    public void DrawEventCard(int playerId)
    {
        if (_eventDeck.Count == 0) return;
        
        CurrentEventCardId = _eventDeck[0];
        _eventDeck.RemoveAt(0);
        
        OnEventCardDrawn?.Invoke(CurrentEventCardId);
        
        // Sync to existing phase manager
        var eventCard = CardRegistry.Instance.GetEventCard(CurrentEventCardId);
        TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.EventCardDrawn, eventCard));
    }
    
    public void DrawTargetCard(int playerId)
    {
        // Combine state and institution decks
        var combinedDeck = new List<string>();
        combinedDeck.AddRange(_stateDeck);
        combinedDeck.AddRange(_institutionDeck);
        
        if (combinedDeck.Count == 0) return;
        
        CurrentTargetCardId = combinedDeck[0];
        
        // Remove from appropriate deck
        _stateDeck.Remove(CurrentTargetCardId);
        _institutionDeck.Remove(CurrentTargetCardId);
        
        OnTargetCardDrawn?.Invoke(CurrentTargetCardId);
        
        // Sync to existing phase manager
        var card = CardRegistry.Instance.GetCard(CurrentTargetCardId);
        TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.TargetCardDrawn, card));
    }
    
    public void SaveEventCard(int playerId, string eventCardId)
    {
        var playerState = GetPlayerState(playerId);
        if (playerState == null) return;
        
        playerState.HeldEventCardId = eventCardId;
        CurrentEventCardId = null;
        
        OnPlayerStateChanged?.Invoke(playerState);
        
        // Sync to existing Player
        var player = GetPlayer(playerId);
        var eventCard = CardRegistry.Instance.GetEventCard(eventCardId);
        if (player != null && eventCard != null)
        {
            player.SaveEvent(eventCard);
            TurnFlowBus.Instance.Raise(new MainStageEvent(MainStage.EventCardSaved, eventCard));
        }
    }
    
    public void ApplyEventCard(int playerId, string eventCardId)
    {
        var eventCard = CardRegistry.Instance.GetEventCard(eventCardId);
        var player = GetPlayer(playerId);
        
        if (eventCard != null && player != null && _gameManager?.mainPhase != null)
        {
            // Use existing event manager
            _gameManager.mainPhase.EventManager.ApplyEvent(player, eventCard);
        }
        
        CurrentEventCardId = null;
    }
    
    public void SelectChallengeState(int playerId, string stateCardId)
    {
        // Implement challenge state selection logic
        // This would interact with existing EventManager
    }
    
    public void MoveToNextPlayer()
    {
        CurrentPlayerIndex = (CurrentPlayerIndex + 1) % _playerStates.Count;
        UpdateCurrentPlayer();
    }
    
    private void UpdateCurrentPlayer()
    {
        var playerIds = _playerStates.Keys.ToList();
        if (CurrentPlayerIndex >= 0 && CurrentPlayerIndex < playerIds.Count)
        {
            CurrentPlayerId = playerIds[CurrentPlayerIndex];
            OnCurrentPlayerChanged?.Invoke(CurrentPlayerId);
        }
    }
    
    public void ShuffleDecks()
    {
        _stateDeck.ShuffleInPlace();
        _institutionDeck.ShuffleInPlace();
        _eventDeck.ShuffleInPlace();
        _actorDeck.ShuffleInPlace();
    }
    
    #endregion
    
    #region Helper Methods
    
    private Player GetPlayer(int playerId)
    {
        if (_gameManager == null) return null;
        return _gameManager.players.FirstOrDefault(p => p.playerID == playerId);
    }
    
    #endregion
    
    #region Serialization (for network)
    
    public byte[] SerializeState()
    {
        // Implement full state serialization for network sync
        // This would serialize all players, decks, current cards, etc.
        return new byte[0];
    }
    
    public void DeserializeState(byte[] data)
    {
        // Implement state deserialization
        // Clients would receive this from server to sync their state
    }
    
    #endregion
}