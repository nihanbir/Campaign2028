using UnityEngine.UI;
using UnityEngine;

public class DisplayCard : MonoBehaviour
{
    void Start()
    {
        if (SetupPhaseGameManager.Instance == null || SetupPhaseGameManager.Instance.players.Count == 0)
        {
            Debug.Log("GameManager or players not initialized.");
            return;
        }
    }

    public virtual void UpdateUI()
    {
        
    }
}


