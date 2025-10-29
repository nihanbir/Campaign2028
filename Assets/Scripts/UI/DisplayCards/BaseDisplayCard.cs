
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BaseDisplayCard<T> : MonoBehaviour where T : Card
{
    [Header("Card Type")]
    public Image artworkImage;
    protected T cardData;
    
    void Start()
    {
        if (GameManager.Instance == null || GameManager.Instance.players.Count == 0)
        {
            Debug.Log("GameManager or players not initialized.");
            return;
        }
    }
    
    public virtual void SetCard(T card)
    {
        cardData = card;
        artworkImage.sprite = cardData.artwork;
    }
}


