using System.Collections.Generic;
using UnityEngine;

public abstract class GameManagerBase : MonoBehaviour
{
    [Header("Decks")]
    [SerializeField] protected GameDeckSO gameDeckData;
    [HideInInspector] public List<StateCard> stateDeck;
    [HideInInspector] public List<InstitutionCard> institutionDeck;
    [HideInInspector] public List<EventCard> eventDeck;
    [HideInInspector] public List<ActorCard> actorDeck;
    [HideInInspector] public List<AllegianceCard> allegianceDeck;

    [Header("Players")]
    [SerializeField] protected int maxPlayers = 6;
    [HideInInspector] public List<Player> players;
    [HideInInspector] public int currentPlayerIndex = 0;

    public Player CurrentPlayer => players[currentPlayerIndex];
    
    protected virtual void Awake()
    {
        players = new List<Player>(FindObjectsOfType<Player>());
    }

    protected void LoadDecks()
    {
        if (!gameDeckData)
        {
            Debug.LogError("GameDeckSO not assigned!");
            return;
        }

        stateDeck = gameDeckData.GetStateDeck();
        institutionDeck = gameDeckData.GetInstitutionDeck();
        eventDeck = gameDeckData.GetEventDeck();
        actorDeck = gameDeckData.GetActorDeck();
        allegianceDeck = gameDeckData.GetAllegianceDeck();
    }
}