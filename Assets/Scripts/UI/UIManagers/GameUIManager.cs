using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Dice & Actions")]
    [SerializeField] private Sprite[] diceFaces;
    [SerializeField] private TextMeshProUGUI phaseText;
    
    [Header("Phase UIs")]
    [SerializeField] public UM_SetupPhase setupUI;
    [SerializeField] public UM_MainPhase mainUI;
    
    private UM_BasePhase _currentPhaseManager;
    
    // Add others (CivilWarUI, GameOverUI) as needed

    public int DiceRoll { get; private set; }
    
    public event Action<int> OnRolledDice;

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
        OnRolledDice?.Invoke(DiceRoll);
    }

    public void SetDiceSprite(Image diceImage)
    {
        diceImage.sprite = diceFaces[DiceRoll - 1];
    }

    #endregion
}