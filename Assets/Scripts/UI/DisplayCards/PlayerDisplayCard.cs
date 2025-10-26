using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayerDisplayCard : MonoBehaviour, IPointerClickHandler
{
    [Header("Card Type")]
    public CardDisplayType displayType;
    
    [Header("Common Elements")]
    public Player owningPlayer;
    public Image diceImage;
    
    [Header("Actor Card Elements")]
    public GameObject scorePanel;
    public TextMeshProUGUI nameText;
    public Image artworkImage;
    public TextMeshProUGUI evScoreText;
    public TextMeshProUGUI instScoreText;
    
    private ActorCard _assignedActor;

    void Start()
    {
        if (SetupPhaseGameManager.Instance == null || SetupPhaseGameManager.Instance.players.Count == 0)
        {
            Debug.Log("GameManager or players not initialized.");
            return;
        }
        
        if (diceImage) diceImage.gameObject.SetActive(false);
        if (scorePanel) scorePanel.SetActive(false);
    }

    public void SetActor(ActorCard actor)
    {
        _assignedActor = actor;
        displayType = CardDisplayType.UnassignedActor;
        UpdateUI();
    }

    public void SetOwnerPlayer(Player player)
    {
        owningPlayer = player;
        
        if (displayType == CardDisplayType.UnassignedPlayer && nameText)
        {
            nameText.text = $"Player {player.playerID}";
        }
        
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (_assignedActor != null && displayType != CardDisplayType.UnassignedPlayer)
        {
            UpdateActorUI();
        }
    }

    private void UpdateActorUI()
    {
        if (_assignedActor == null) return;
        
        if (nameText) nameText.text = _assignedActor.cardName;
        if (artworkImage) artworkImage.sprite = _assignedActor.artwork;
        if (evScoreText) evScoreText.text = _assignedActor.evScore.ToString();
        if (instScoreText) instScoreText.text = _assignedActor.instScore.ToString();
    }

    public void SetRolledDiceImage()
    {
        if (diceImage)
        {
            diceImage.gameObject.SetActive(true);
            GameUIManager.Instance.SetDiceSprite(diceImage);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        switch (displayType)
        {
            case CardDisplayType.UnassignedActor:
                HandleActorCardClick();
                break;
            case CardDisplayType.UnassignedPlayer:
                HandlePlayerCardClick();
                break;
            case CardDisplayType.AssignedActor:
                // Already assigned, no action
                break;
        }
    }

    private void HandleActorCardClick()
    {
        if (owningPlayer != null) return; // Already assigned
        SetupPhaseUIManager.Instance.SelectActorCard(this);
    }

    private void HandlePlayerCardClick()
    {
        SetupPhaseUIManager.Instance.AssignSelectedActorToPlayer(owningPlayer, this);
    }

    public ActorCard GetActorCard()
    {
        return _assignedActor;
    }

    public void ConvertToAssignedActor(ActorCard actor)
    {
        _assignedActor = actor;
        displayType = CardDisplayType.AssignedActor;
        UpdateUI();
    }

    public void ShowDice(bool show)
    {
        if (diceImage) diceImage.gameObject.SetActive(show);
    }
}

public enum CardDisplayType
{
    UnassignedPlayer,
    UnassignedActor,
    AssignedActor
}