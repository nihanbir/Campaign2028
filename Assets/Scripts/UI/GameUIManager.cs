using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance;
    
    [Header("Dice & Actions")]
    public Sprite[] diceFaces; // 6 sprites for dice faces 1-6
    public TextMeshProUGUI phaseText;
    
    private int _diceRoll;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        MainPhaseUIManager.Instance.mainGamePhase.SetActive(false);
        
        UpdateGamePhase(GamePhase.Setup);
    }
    
    public void UpdateGamePhase(GamePhase phase)
    {
        switch (phase)
         {
             case GamePhase.Setup:
                 phaseText.text = "Setup Phase";
                 SetupPhaseUIManager.Instance.setupGamephase.SetActive(true);
                 break;
             case GamePhase.MainGame:
                 phaseText.text = "Main Game";
                 break;
             case GamePhase.CivilWar:
                 phaseText.text = "⚔️ CIVIL WAR ⚔️";
                 phaseText.color = Color.red;
                 break;
             case GamePhase.GameOver:
                 phaseText.text = "Game Over";
                 break;
         }
    }
    
    #region Dice & Actions
    public void OnRollDiceClicked(Button diceButton)
    {
        _diceRoll = Random.Range(1, 7);
        switch (_diceRoll)
        {
            case 1:
                diceButton.image.sprite = diceFaces[0];
                return;
            case 2:
                diceButton.image.sprite = diceFaces[1];
                return;
            case 3:
                diceButton.image.sprite = diceFaces[2];
                return;
            case 4:
                diceButton.image.sprite = diceFaces[3];
                return;
            case 5:
                diceButton.image.sprite = diceFaces[4];
                return;
            case 6:
                diceButton.image.sprite = diceFaces[5];
                return;
        }
    }
    #endregion Dice & Actions
}