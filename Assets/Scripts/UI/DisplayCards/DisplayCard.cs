using UnityEngine.UI;
using UnityEngine;

public class DisplayCard : MonoBehaviour
{
    void Start()
    {
        if (GameManager.Instance == null || GameManager.Instance.players.Count == 0)
        {
            Debug.Log("GameManager or players not initialized.");
            return;
        }
    }

    public virtual void UpdateUI()
    {
        
    }
}


