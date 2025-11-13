using System;
using DG.Tweening;
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
    [SerializeField] private Image diceImage;
    
    [Header("Phase UIs")]
    [SerializeField] public UM_SetupPhase setupUI;
    [SerializeField] public UM_MainPhase mainUI;
    
    // Add others (CivilWarUI, GameOverUI) as needed

    private Button _currentRollButton;
    
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        TurnFlowBus.Instance.OnEvent += OnTurnEvent;
        
    }

    private void OnTurnEvent(IGameEvent e)
    {
        if (e is TurnEvent t)
        {
            if (t.stage == TurnStage.RollDiceRequest)
            {
                var data = (PlayerRolledData)t.Payload;
                OnPlayerRolledDice(data.Player, data.Roll);
            }
        }
    }

    #region Dice & Actions
    
    public void RegisterRollButtonAndDiceImage(Button button, Image diceIMG)
    {
        if (_currentRollButton != null)
            _currentRollButton.onClick.RemoveListener(OnRollDiceClicked);

        _currentRollButton = button;
        _currentRollButton.onClick.AddListener(OnRollDiceClicked);

        diceImage = diceIMG;
    }

    private void OnRollDiceClicked()
    {
        TurnFlowBus.Instance.Raise(new TurnEvent(TurnStage.RollDiceRequest, new RollDiceRequest()));
    }

    private void OnPlayerRolledDice(Player player, int roll)
    {
        
        player.PlayerDisplayCard.SetRolledDiceImage(diceFaces[roll - 1]);
        
        diceImage.sprite = diceFaces[roll - 1];
        
        diceImage.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.8f);
    }

    #endregion
}