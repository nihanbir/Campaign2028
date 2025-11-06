
using System;
using UnityEngine;

public class PhaseUIManager : MonoBehaviour
{
    protected GameManager game;
    // protected GM_BasePhase currentPhase;
    
    private void Awake()
    {
        game = GameManager.Instance;
        // game.OnPhaseChanged += SetCurrentPhase;
    }

    // private void SetCurrentPhase(Type obj)
    // {
    //     currentPhase = obj;
    // }
}
