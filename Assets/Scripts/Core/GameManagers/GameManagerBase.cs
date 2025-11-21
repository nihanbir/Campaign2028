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
    [SerializeField] private int placeHumanAtIndex = 0;
    [SerializeField] protected int maxPlayers = 6;
    [HideInInspector] public List<Player> players;
    [HideInInspector] public int currentPlayerIndex = 0;

    public Player CurrentPlayer => players[currentPlayerIndex];
    
    protected virtual void Awake()
    {
        SetPlayers(placeHumanAtIndex);
        
        LoadDecks();
    }

    private void SetPlayers(int index)
    {
        players = new List<Player>(FindObjectsByType<Player>( FindObjectsInactive.Include, FindObjectsSortMode.None));
        
        // Find the human player (no AI component attached)
        Player human = players.FirstOrDefault(p => p.GetComponent<AIPlayer>() == null);

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