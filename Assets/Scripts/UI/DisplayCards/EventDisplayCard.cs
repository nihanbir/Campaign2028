
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EventDisplayCard : BaseDisplayCard
{
    [Header("Card")]
    public CardStatus cardStatus = CardStatus.Unrevealed;
    private EventCard _eventCard;
    public Button saveButton;
    public Button playButton;
    
    [Header("Event Card Elements")]
    public Image artworkImage;

    public override void OnPointerClick(PointerEventData eventData) { }
    
    void Start()
    {
        if (GameManager.Instance == null || GameManager.Instance.players.Count == 0)
        {
            Debug.Log("GameManager or players not initialized.");
            return;
        }
    }
    
    public void SetEventCard(EventCard eventCard)
    {
        _eventCard = eventCard;
        InitializeButtons();
        RevealCard();
    }

    private void InitializeButtons()
    {
        if (saveButton)
        {
            saveButton.onClick.AddListener(OnCardSaved);
            saveButton.gameObject.SetActive(false);
        }
        
        if (playButton)
        {
            playButton.onClick.AddListener(OnCardPlayed);
            playButton.gameObject.SetActive(false);
        }
    }

    private void RevealCard()
    {
        cardStatus = CardStatus.Revealed;
        artworkImage.sprite = _eventCard.artwork;
        SetButtonsVisible(true);
    }
    
    private void OnCardPlayed()
    {
        GameManager.Instance.mainPhase.ApplyEventEffect(GameManager.Instance.CurrentPlayer, _eventCard);
    }

    private void OnCardSaved()
    {
        GameManager.Instance.mainPhase.TrySaveEvent(GameManager.Instance.CurrentPlayer, _eventCard);
    }

    public void SetButtonsVisible(bool visible)
    {
        playButton.gameObject.SetActive(visible);
        saveButton.gameObject.SetActive(_eventCard.canSave && visible);
    }
}

public enum CardStatus
{
    Unrevealed,
    Revealed,
}