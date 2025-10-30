
using UnityEngine;
using UnityEngine.UI;

public class EventDisplayCard : BaseDisplayCard<EventCard>
{
    [Header("Card")]
    public Button saveButton;
    public Button playButton;
    
    public override void SetCard(EventCard card)
    {
        base.SetCard(card);
        InitializeButtons();
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
    
    private void OnCardSaved()
    {
        GameManager.Instance.mainPhase.TrySaveEvent(cardData);
    }
    private void OnCardPlayed()
    {
        GameManager.Instance.mainPhase.EventManager.ApplyEvent(cardData, GameManager.Instance.CurrentPlayer);
    }
    
    public void SetButtonsVisible(bool visible)
    {
        playButton.gameObject.SetActive(visible);
        playButton.interactable = visible;

        var canSave = cardData.canSave && visible;
        saveButton.gameObject.SetActive(canSave);
        saveButton.interactable = canSave;
        
    }
}