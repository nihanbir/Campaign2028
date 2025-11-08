using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class OwnedCardsPanel : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Slide Settings")]
    public float slideDuration = 0.5f;
    public Ease slideEase = Ease.OutBack;

    [Header("References")]
    public RectTransform panel; // assign this panel (usually same object)
    public float hiddenY = -800f; // how far below screen when hidden
    public float visibleY = 0f;   // target Y when visible

    private bool isVisible = false;
    private Vector2 dragStartPos;
    private Vector2 panelStartPos;
    private bool isDragging;

    private void Start()
    {
        if (!panel) panel = GetComponent<RectTransform>();
        // start hidden
        Vector2 start = panel.anchoredPosition;
        start.y = hiddenY;
        panel.anchoredPosition = start;
    }

    public void TogglePanel()
    {
        if (isVisible) Hide();
        else Show();
    }

    public void Show()
    {
        isVisible = true;
        panel.DOKill();
        panel.DOAnchorPosY(visibleY, slideDuration)
             .SetEase(slideEase);
    }

    public void Hide()
    {
        isVisible = false;
        panel.DOKill();
        panel.DOAnchorPosY(hiddenY, slideDuration)
             .SetEase(Ease.InBack);
    }

    // --- Optional drag-to-close behaviour ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        dragStartPos = eventData.position;
        panelStartPos = panel.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        float deltaY = eventData.position.y - dragStartPos.y;
        Vector2 newPos = panelStartPos + new Vector2(0, deltaY);
        newPos.y = Mathf.Clamp(newPos.y, hiddenY, visibleY);
        panel.anchoredPosition = newPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        // if dragged down more than halfway -> close
        if (panel.anchoredPosition.y < visibleY * 0.5f)
            Hide();
        else
            Show();
    }
}
