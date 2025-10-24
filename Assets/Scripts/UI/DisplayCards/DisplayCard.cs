using UnityEngine.UI;
using UnityEngine;

public class DisplayCard : MonoBehaviour
{
    
    public Player owningPlayer;
    public Image diceImage;
    
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
    
    public void SetRolledDiceImage(int diceRoll)
    {
        diceImage.gameObject.SetActive(true);
        Debug.Log(diceRoll);
        GameUIManager.Instance.SetDiceSprite(diceImage);
    }
}


