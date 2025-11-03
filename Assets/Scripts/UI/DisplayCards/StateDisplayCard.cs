using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class StateDisplayCard : BaseDisplayCard<StateCard>, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public static event Action<StateDisplayCard> OnCardHeld;
    public static event Action<StateDisplayCard> OnCardHighlighted;
    
    [SerializeField] private float holdDuration = 1f;
    
    private bool _isClickable;
    private bool _isHolding;
    private bool _isSelected;
    private float _holdTimer;
    
    
    private void Update()
    {
        if ( _isClickable && _isHolding)
        {
            _holdTimer += Time.deltaTime;
            if (_holdTimer >= holdDuration)
            {
                _isHolding = false; // prevent multiple fires
                OnCardHeld?.Invoke(this);
            }
        }
    }
    
    public void SetClickable(bool value) => _isClickable = value;
    public void SetIsSelected(bool value)
    {
        _isSelected = value;
        if (value == true)
        {
            Highlight();
        }
        else
        {
            RemoveHighlight();
        }
    } 
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isClickable) return;
        if (_isSelected)
        {
            SetIsSelected(false);
        }
        else
        {
            SetIsSelected(true);
            OnCardHighlighted?.Invoke(this);
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_isClickable) return;
        if (!_isSelected) return;
        _isHolding = true;
        _holdTimer = 0f;
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isClickable) return;
        if (!_isSelected) return;
        
        _isHolding = false;
        _holdTimer = 0f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_isClickable) return;
        
        _isHolding = false;
        _holdTimer = 0f;
    }

    
}