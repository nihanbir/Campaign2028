using UnityEngine;
using UnityEngine.EventSystems;
using System;

public abstract class SelectableDisplayCard<T> : BaseDisplayCard<T>, ISelectableDisplayCard,
    IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    where T : Card
{
    public static event Action<ISelectableDisplayCard> OnCardSelected;
    public static event Action<T> OnCardHeld;

    [SerializeField] protected float holdDuration = 1f;
    protected bool isClickable;
    protected bool isHolding;
    protected bool isSelected;
    protected float holdTimer;

    protected virtual void Update()
    {
        if (isClickable && isHolding)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= holdDuration)
            {
                isHolding = false;
                OnCardHeld?.Invoke(GetCard());
            }
        }
    }

    public virtual void SetClickable(bool value) => isClickable = value;

    public virtual void SetIsSelected(bool value)
    {
        isSelected = value;
        if (value)
            Highlight();
        else
            RemoveHighlight();
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (!isClickable) return;

        SetIsSelected(!isSelected);

        if (isSelected)
            OnCardSelected?.Invoke(this);
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (!isClickable || !isSelected) return;
        isHolding = true;
        holdTimer = 0f;
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        if (!isClickable || !isSelected) return;
        isHolding = false;
        holdTimer = 0f;
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        if (!isClickable) return;
        isHolding = false;
        holdTimer = 0f;
    }
}