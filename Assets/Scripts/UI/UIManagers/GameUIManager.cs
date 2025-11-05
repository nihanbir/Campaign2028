using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance;

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

    public void OnTransitionToPhase(GamePhase phase)
    {
        setupUI.gameObject.SetActive(false);
        mainUI.gameObject.SetActive(false);

        //TODO: not okay to initialize UIs over and over again
        switch (phase)
        {
            case GamePhase.Setup:
                phaseText.text = "Setup Phase";
                setupUI.gameObject.SetActive(true);
                setupUI.InitializePhaseUI();
                break;

            case GamePhase.MainGame:
                phaseText.text = "Main Game";
                mainUI.gameObject.SetActive(true);
                mainUI.InitializePhaseUI();
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
        DiceRoll = Random.Range(1, 7);
        SetDiceSprite(diceButton.image);
    }

    public void SetDiceSprite(Image diceImage)
    {
        diceImage.sprite = diceFaces[DiceRoll - 1];
    }

    #endregion
}