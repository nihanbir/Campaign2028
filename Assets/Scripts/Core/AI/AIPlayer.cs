using UnityEngine;

public class AIPlayer : Player
{
    [Header("AI Settings")]
    public float decisionDelayMin = 0.5f;
    public float decisionDelayMax = 2f;
}