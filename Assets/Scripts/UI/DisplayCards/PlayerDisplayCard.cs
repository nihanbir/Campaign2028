using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayerDisplayCard : BaseDisplayCard<ActorCard>, IPointerClickHandler
{
    [Header("Common Elements")]
    public Player owningPlayer;
    public Image diceImage;
    public CardDisplayType displayType;
    
    [Header("Actor Card Elements")]
    public GameObject scorePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI evScoreText;
    public TextMeshProUGUI instScoreText;
    
    [Header("Highlight Settings")]
    public float highlightScale = 1.2f;
    
    private Vector3 _originalScale;
    private int _originalSiblingIndex;
    private bool _isHighlighted = false;

    void Start()
    {
        if (GameManager.Instance == null || GameManager.Instance.players.Count == 0)
        {
            Debug.Log("GameManager or players not initialized.");
            return;
        }
        
        if (diceImage) diceImage.gameObject.SetActive(false);
        if (GameManager.Instance.CurrentPhase != GamePhase.MainGame)
        {
            if (scorePanel) scorePanel.SetActive(false);
        }
    }

    public override void SetCard(ActorCard card)
    {
        cardData = card;
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
        if (cardData != null && displayType != CardDisplayType.UnassignedPlayer)
        {
            UpdateActorUI();
        }
    }

    private void UpdateActorUI()
    {
        if (cardData == null) return;
        
        nameText.text = cardData.cardName;
        artworkImage.sprite = cardData.artwork;
        UpdateScore();
    }

    public void SetRolledDiceImage()
    {
        if (diceImage)
        {
            ShowDice(true);
            GameUIManager.Instance.SetDiceSprite(diceImage);
        }
    }

    #region Highlight Methods
    
    public void Highlight()
    {
        _originalScale = transform.localScale;
        _originalSiblingIndex = transform.GetSiblingIndex();
        
        if (_isHighlighted) return;
        _isHighlighted = true;
        
        // Move to front
        transform.SetAsLastSibling();
        
        transform.localScale = _originalScale * highlightScale;
    }
    
    public void RemoveHighlight()
    {
        if (!_isHighlighted) return;
        _isHighlighted = false;
        
        // Restore original sibling index
        transform.SetSiblingIndex(_originalSiblingIndex);
        
        transform.localScale = _originalScale;
    }
    
    #endregion

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
        GameUIManager.Instance.setupUI.SelectActorCard(this);
    }

    private void HandlePlayerCardClick()
    {
        GameUIManager.Instance.setupUI.AssignSelectedActorToPlayer(owningPlayer, this);
    }

    public ActorCard GetActorCard()
    {
        return cardData;
    }

    public void ConvertToAssignedActor(ActorCard actor)
    {
        cardData = actor;
        displayType = CardDisplayType.AssignedActor;
        UpdateUI();
    }

    public void ShowDice(bool show)
    {
        if (diceImage) diceImage.gameObject.SetActive(show);
    }

    public void UpdateScore()
    {
        evScoreText.text = cardData.evScore.ToString();
        instScoreText.text = cardData.instScore.ToString();
    }
}

public enum CardDisplayType
{
    UnassignedPlayer,
    UnassignedActor,
    AssignedActor,
}