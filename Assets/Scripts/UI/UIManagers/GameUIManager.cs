using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Dice & Actions")]
    [SerializeField] private Sprite[] diceFaces;
    [SerializeField] private TextMeshProUGUI phaseText;

    [Header("Phase UIs")]
    [SerializeField] public SetupPhaseUIManager setupUI;
    [SerializeField] public MainPhaseUIManager mainUI;
    // Add others (CivilWarUI, GameOverUI) as needed

    public int DiceRoll { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
    }
    
    //TODO: add an event for roll dice clicked

    #region Dice & Actions

    public void OnRollDiceClicked(Button diceButton)
    {
        DiceRoll = Random.Range(1, 7);
        SetDiceSprite(diceButton.image);
    }

    public void SetDiceSprite(Image diceImage)
    {
        diceImage.sprite = diceFaces[DiceRoll - 1];
    }

    #endregion
}