
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
        artworkImage.sprite = cardData.artwork;
        SetButtonsVisible(true);
    }
    
    private void OnCardPlayed()
    {
        GameManager.Instance.mainPhase.ApplyEventEffect(GameManager.Instance.CurrentPlayer, cardData);
    }

    private void OnCardSaved()
    {
        GameManager.Instance.mainPhase.TrySaveEvent(GameManager.Instance.CurrentPlayer, cardData);
    }

    public void SetButtonsVisible(bool visible)
    {
        playButton.gameObject.SetActive(visible);
        saveButton.gameObject.SetActive(cardData.canSave && visible);
    }
}