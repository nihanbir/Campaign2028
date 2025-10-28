
using System.Collections.Generic;
using UnityEngine;

public class MainPhaseAIManager
{
    private readonly AIManager aiManager;
    private readonly List<AIPlayer> aiPlayers;

    public MainPhaseAIManager(AIManager manager)
    {
        aiManager = manager;
        aiPlayers = aiManager.aiPlayers;
    }
}
