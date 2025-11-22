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
    [SerializeField] private Button playAgainButton;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float scaleInDuration = 0.8f;
    
    private void Awake()
    {
        gameOverScreen.SetActive(false);
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    public void Initialize()
    {
        TurnFlowBus.Instance.OnEvent += HandleTurnEvent;
        
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
    }

    private void OnDestroy()
    {
        if (TurnFlowBus.Instance != null)
            TurnFlowBus.Instance.OnEvent -= HandleTurnEvent;
        
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveAllListeners();
        
        if (playAgainButton != null)
            playAgainButton.onClick.RemoveAllListeners();
    }

    protected override void HandleTurnEvent(IGameEvent e)
    {
        if (e is PhaseChangeEvent m && m.stage == GamePhase.GameOver)
        {
            var data = (GameOverData)m.payload;
            StartCoroutine(ShowGameOverRoutine(data));
        }
    }

    private IEnumerator ShowGameOverRoutine(GameOverData data)
    {
        // Wait a moment before showing game over
        yield return new WaitForSeconds(1f);
        
        // Set up UI content
        SetupGameOverContent(data);
        
        // Activate screen
        gameOverScreen.SetActive(true);
        
        // Animate in
        yield return AnimateGameOverIn();
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
    }

    private IEnumerator AnimateGameOverIn()
    {
        if (canvasGroup == null) yield break;
        
        // Start invisible and small
        canvasGroup.alpha = 0f;
        transform.localScale = Vector3.one * 0.5f;
        
        bool done = false;
        
        Sequence seq = DOTween.Sequence();
        
        // Fade in
        seq.Append(canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutCubic));
        
        // Scale up with bounce
        seq.Join(transform.DOScale(1f, scaleInDuration).SetEase(Ease.OutBack));
        
        // Pulse winner text
        if (winnerText != null)
        {
            seq.Join(winnerText.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 5, 1f).SetDelay(0.3f));
        }
        
        seq.OnComplete(() => done = true);
        
        while (!done)
            yield return null;
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("Main Menu clicked - implement scene loading");
        // TODO: Implement main menu scene loading
        // UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private void OnPlayAgainClicked()
    {
        Debug.Log("Play Again clicked - implement game restart");
        // TODO: Implement game restart
        // UnityEngine.SceneManagement.SceneManager.LoadScene(
        //     UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        // );
    }
}