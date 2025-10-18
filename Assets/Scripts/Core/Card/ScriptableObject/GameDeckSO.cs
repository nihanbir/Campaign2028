using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Game Deck", menuName = "Campaign 2028/Game Deck Container")]
public class GameDeckSO : ScriptableObject
{
    [Header("All Cards")]
    public List<StateCardSO> allStates = new List<StateCardSO>();
    public List<InstitutionCardSO> allInstitutions = new List<InstitutionCardSO>();
    public List<EventCardSO> allEvents = new List<EventCardSO>();
    public List<ActorCardSO> allActors = new List<ActorCardSO>();
    public List<AllegianceCardSO> allAllegiances = new List<AllegianceCardSO>();
    
    public List<StateCard> GetStateDeck()
    {
        List<StateCard> deck = new List<StateCard>();
        foreach (var stateSO in allStates)
        {
            deck.Add(stateSO.ToCard());
        }
        return deck;
    }
    
    public List<InstitutionCard> GetInstitutionDeck()
    {
        List<InstitutionCard> deck = new List<InstitutionCard>();
        foreach (var instSO in allInstitutions)
        {
            deck.Add(instSO.ToCard());
        }
        return deck;
    }
    
    public List<EventCard> GetEventDeck()
    {
        List<EventCard> deck = new List<EventCard>();
        foreach (var eventSO in allEvents)
        {
            deck.Add(eventSO.ToCard());
        }
        return deck;
    }
    
    public List<ActorCard> GetActorDeck()
    {
        List<ActorCard> deck = new List<ActorCard>();
        foreach (var actorSO in allActors)
        {
            deck.Add(actorSO.ToCard());
        }
        return deck;
    }
    
    public List<AllegianceCard> GetAllegianceDeck()
    {
        List<AllegianceCard> deck = new List<AllegianceCard>();
        foreach (var allegSO in allAllegiances)
        {
            deck.Add(allegSO.ToCard());
        }
        return deck;
    }
}