using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Game Deck", menuName = "Campaign 2028/Game Deck Container")]
public class GameDeckSO : ScriptableObject
{
    [Header("All Cards")]
    public List<StateCardSO> allStates = new List<StateCardSO>();
    public List<InstitutionCardSO> allInstitutions = new List<InstitutionCardSO>();
    public List<ActorCardSO> allActors = new List<ActorCardSO>();
    public List<AllegianceCardSO> allAllegiances = new List<AllegianceCardSO>();
    [ReadOnly] public List<EventCardSO> allEvents = new List<EventCardSO>();
    
    [Header("Event Categories")]
    public List<EventCardSO> extraRollEvents = new();
    public List<EventCardSO> needTwoEvents = new();
    public List<EventCardSO> challengeEvents = new();
    public List<EventCardSO> loseTurnEvents = new();
    public List<EventCardSO> noImpactEvents = new();
    public List<EventCardSO> alternativeStatesEvents = new();
    public List<EventCardSO> teamBasedEvents = new();
    
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
        BuildAllEventsDeck();
        List<EventCard> deck = new List<EventCard>();
        foreach (var eventSO in allEvents)
        {
            deck.Add(eventSO.ToCard());
        }
        return deck;
    }

    private void BuildAllEventsDeck()
    {
        allEvents.Clear();
        allEvents.AddRange(extraRollEvents);
        allEvents.AddRange(needTwoEvents);
        allEvents.AddRange(challengeEvents);
        allEvents.AddRange(loseTurnEvents);
        allEvents.AddRange(noImpactEvents);
        allEvents.AddRange(alternativeStatesEvents);
        allEvents.AddRange(teamBasedEvents);
        
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