using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public string playerName;
    public int playerID;
    public ActorCard assignedActor;
    public AllegianceCard allegiance;

    private GameManager _gameManager;
    
    // Victory tracking
    public int totalElectoralVotes;
    public List<StateCard> capturedStates = new List<StateCard>();
    public List<InstitutionCard> capturedInstitutions = new List<InstitutionCard>();
    
}