using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the Game Over screen display
/// </summary>
public class UM_GameOver : UM_BasePhase
{
    public override GamePhase PhaseType => GamePhase.GameOver;
    
    [Header("UI Elements")]
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private TextMeshProUGUI victoryTypeText;
    [SerializeField] private TextMeshProUGUI detailsText;
    [SerializeField] private Image winnerActorImage;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("Buttons")]
    [SerializeField] private Button mainMenuButton;

    protected override void SubscribeToPhaseEvents()
    {
        base.SubscribeToPhaseEvents();

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
    }

    protected override void UnsubscribeToPhaseEvents()
    {
        base.UnsubscribeToPhaseEvents();
        
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveAllListeners();
    }

    protected override void HandleTurnEvent(IGameEvent e)
    {
        //TODO:beginphase
        if (e is PhaseChangeEvent m && m.stage == GamePhase.GameOver)
        {
            var data = (GameOverData)m.payload;
            ShowGameOverRoutine(data);
        }
    }

    private void ShowGameOverRoutine(GameOverData data)
    {
       // Set up UI content
        SetupGameOverContent(data);
        
        // Activate screen
        gameOverScreen.SetActive(true);
    }

    private void SetupGameOverContent(GameOverData data)
    {
        var winner = data.Winner;
        
        // Set winner name
        if (winnerText != null)
        {
            winnerText.text = $"{winner.PlayerName} Wins!";
        }
        
        // Set victory type
        if (victoryTypeText != null)
        {
            string victoryMessage = data.VictoryType switch
            {
                VictoryType.ElectoralVotes => "Electoral Victory",
                VictoryType.Institutions => "Institutional Control",
                _ => "Victory!"
            };
            victoryTypeText.text = victoryMessage;
        }
        
        // Set details
        if (detailsText != null)
        {
            string details = data.VictoryType switch
            {
                VictoryType.ElectoralVotes => 
                    $"Secured {winner.ElectoralVotes} Electoral Votes\n(Required: 290)",
                VictoryType.Institutions => 
                    $"Controls {winner.InstitutionCount} Institutions\n(Including the CIA)",
                _ => ""
            };
            detailsText.text = details;
        }
        
        // Set actor image
        if (winnerActorImage != null && winner.assignedActor != null)
        {
            winnerActorImage.sprite = winner.assignedActor.artwork;
        }
        
        InitUI();
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("Main Menu clicked - implement scene loading");
        // TODO: Implement main menu scene loading
        // UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}