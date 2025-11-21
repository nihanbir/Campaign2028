using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class GameManagerBase : MonoBehaviour
{
    [Header("Decks")]
    [SerializeField] public GameDeckSO gameDeckData;
    [HideInInspector] public List<StateCard> stateDeck;
    [HideInInspector] public List<InstitutionCard> institutionDeck;
    [HideInInspector] public List<EventCard> eventDeck;
    [HideInInspector] public List<ActorCard> actorDeck;
    [HideInInspector] public List<AllegianceCard> allegianceDeck;

    [Header("Players")]
    [SerializeField] private int humanPlayerCount = 1;
    [SerializeField] private int placeHumanAtIndex = 0;
    [SerializeField] protected int maxPlayers = 6;
    [HideInInspector] public List<Player> players = new();
    [HideInInspector] public int currentPlayerIndex = 0;

    public Player CurrentPlayer => players[currentPlayerIndex];
    
    protected virtual void Awake()
    {
        CreatePlayers(humanPlayerCount);
        LoadDecks();
    }
    
    private void CreatePlayers(int humanCount)
    {
        players.Clear();
        
        // Create human players
        for (int i = 0; i < humanCount; i++)
        {
            players.Add(new Player(i, isAI: false));
        }
        
        // Create AI players to fill up to maxPlayers
        int aiCount = maxPlayers - humanCount;
        for (int i = 0; i < aiCount; i++)
        {
            players.Add(new AIPlayer(humanCount + i));
        }
        
        Debug.Log($"Created {humanCount} human players and {aiCount} AI players");
        SetHumanAtIndex(placeHumanAtIndex);
    }

    private void SetHumanAtIndex(int index)
    {
        // Find the human player
        Player human = players.FirstOrDefault(p => !p.IsAI);

        if (human != null)
        {
            players.Remove(human);
            players.Insert(index, human);
        }
    }

    private void LoadDecks()
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

// === Extensions for lists ===
public static class ListExtensions
{
    public static void ShuffleInPlace<T>(this IList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public static List<T> Shuffled<T>(this IEnumerable<T> source)
        => source.OrderBy(_ => Random.value).ToList();

    public static T PopFront<T>(this IList<T> list)
    {
        T value = list[0];
        list.RemoveAt(0);
        return value;
    }
    
    public static string GetPlayerIDList(this List<Player> playerList)
    {
        return string.Join(", ", playerList.Select(p => p.playerID));
    }
}