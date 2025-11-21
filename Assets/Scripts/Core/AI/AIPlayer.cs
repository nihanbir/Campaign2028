using UnityEngine;

[System.Serializable]
public class AIPlayer : Player
{
    public float decisionDelayMin = 0.5f;
    public float decisionDelayMax = 2f;
    
    public AIPlayer(int id) : base(id, isAI: true)
    {
    }
}