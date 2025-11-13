using UnityEngine;
using UnityEngine.EventSystems;
using System;

public abstract class SelectableDisplayCard<T> : BaseDisplayCard<T>, ISelectableDisplayCard,
    IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    where T : Card
{
    
    //TODO: clean this up later
    public static event Action<ISelectableDisplayCard> OnCardSelected;
    public static event Action<T> OnCardHeld;

    [SerializeField] protected float holdDuration = 1f;
    protected bool isClickable;
    protected bool isHoldable;
    protected bool isHolding;
    protected bool isSelected;
    protected float holdTimer;

    protected virtual void Update()
    {
        if (!isHoldable) return;
        if (!isHolding) return;
        
        //TODO: animate
        holdTimer += Time.deltaTime;
            
        if (holdTimer >= holdDuration)
        {
            isHolding = false;
            //TODO: animate
            TurnFlowBus.Instance.Raise(
                new CardInputEvent(CardInputStage.Held, GetCard())
            );
        }
    }

    public virtual void SetClickable(bool value) => isClickable = value;
    public virtual void SetHoldable(bool value) => isHoldable = value;

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
            TurnFlowBus.Instance.Raise(
                new CardInputEvent(CardInputStage.Clicked, GetCard())
            );
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (!isHoldable) return;
        
        if (!isSelected)
        {
            SetIsSelected(true);
            //TODO: is there a cleaner way?
            if (isSelected)
                TurnFlowBus.Instance.Raise(
                    new CardInputEvent(CardInputStage.Clicked, GetCard())
                );
        }
        
        isHolding = true;
        holdTimer = 0f;
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        //TODO: this?
        if (!isHoldable || !isSelected) return;
        
        isHolding = false;
        holdTimer = 0f;
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        if (!isHoldable) return;
        isHolding = false;
        holdTimer = 0f;
    }
}