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
    
    // Add others (CivilWarUI, GameOverUI) as needed

    public int DiceRoll { get; set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
    }

    //TODO: have one button available at a time and connect that button to here
    
    //TODO: add an event for roll dice clicked

    #region Dice & Actions

    public void OnRollDiceClicked(Button diceButton)
    {
        //TODO: make this generic instead of event stage
        EventCardBus.Instance.Raise(
            new CardEvent(EventStage.RollDiceRequest, new RollDiceRequest())
        );
        
    }
    
    public void PlayerRolled(Image diceImage)
    {
        diceImage.sprite = diceFaces[DiceRoll - 1];
    }
    
    public void SetDiceSprite(Image diceImage)
    {
        diceImage.sprite = diceFaces[DiceRoll - 1];
    }

    #endregion
}