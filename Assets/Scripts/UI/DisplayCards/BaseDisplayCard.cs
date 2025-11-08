
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BaseDisplayCard<T> : MonoBehaviour, IDisplayCard where T : Card
{
    [Header("Card Type")]
    public Image artworkImage;
    protected T cardData;
    
    [Header("Highlight Settings")]
    public float movefrontScale = 1.2f;
    
    protected Vector3 originalScale;
    protected int originalSiblingIndex;
    protected bool isResized = false;
    
    void Start()
    {
        if (GameManager.Instance == null || GameManager.Instance.players.Count == 0)
        {
            Debug.Log("GameManager or players not initialized.");
            return;
        }
    }
    
    public void SetCard(Card card)
    {
        // Safe cast
        if (card is T typedCard)
            SetCard(typedCard);
        else
            Debug.LogError($"Invalid card type {card.GetType().Name} passed to {GetType().Name}");
    }
    
    protected virtual void SetCard(T card)
    {
        cardData = card;
        artworkImage.sprite = cardData.artwork;
    }

    public T GetCard() => cardData;
    
    #region Highlight Methods
    
    public virtual void Highlight()
    {
        if (isResized) return;
        isResized = true;
        
        originalScale = transform.localScale;
        originalSiblingIndex = transform.GetSiblingIndex();
        
        // Move to front
        transform.SetAsLastSibling();
        
        transform.localScale = originalScale * movefrontScale;
    }
    
    public virtual void RemoveHighlight()
    {
        if (!isResized) return;
        isResized = false;
        
        // Restore original sibling index
        transform.SetSiblingIndex(originalSiblingIndex);
        
        transform.localScale = originalScale;
    }
    
    #endregion
}




