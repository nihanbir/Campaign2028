using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Decks")] public GameDeckSO gameDeckData; // Assign in inspector

    [Header("Actor Deck")] public List<ActorCard> actorDeck;

    [Header("Game State")] public int maxPlayers = 6;
    public List<Player> players;
    public int currentPlayerIndex = 0;
    public Player CurrentPlayer => players[currentPlayerIndex];
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        players = new List<Player>(FindObjectsOfType<Player>());
    }

    void Start()
    {
        InitializeActorDeck();
        AssignActors();
    }

    void AssignActors()
    {
        List<ActorCard> availableActors = new List<ActorCard>(actorDeck);
        
        foreach (var player in players)
        {
            if (availableActors.Count == 0)
            {
                Debug.LogWarning("Not enough actor cards for all players!");
                break;
            }

            int randomIndex = Random.Range(0, availableActors.Count);
            ActorCard chosenActor = availableActors[randomIndex];

            player.assignedActor = chosenActor;
            
            availableActors.RemoveAt(randomIndex);
            Debug.Log($"{player.playerName} assigned actor: {chosenActor.cardName}");
        }
    }
    
    void InitializeActorDeck()
    {
        actorDeck = gameDeckData.GetActorDeck();
    }
}