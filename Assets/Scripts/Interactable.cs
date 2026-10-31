using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public abstract class Interactable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
IPointerClickHandler
{
    [SerializeField] private bool isInteractable = true;

    public event Action<Interactable> HoverEntered;
    public event Action<Interactable> HoverExited;
    public event Action<Interactable> Clicked;

    public bool IsHovered{get; private set;}

    public bool IsInteractable
    {
        get => isInteractable;

        set
        {
            if (isInteractable == value) return;
            isInteractable = value;

            if(!isInteractable && IsHovered) ExitHover();
        }

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInteractable || IsHovered) return;
        IsHovered = true;
        OnHoverEnter();
        HoverEntered?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(!IsHovered) return;
        ExitHover();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(!isInteractable) return;
        if(!CanHandleClick(eventData)) return;
        OnClick(eventData);
        Clicked?.Invoke(this);
    }

    ///Overriding it can let another mouse button be used
    ///instead of the left click default
    protected virtual bool CanHandleClick(PointerEventData eventData)
        => eventData.button == PointerEventData.InputButton.Left;

    protected virtual void OnHoverEnter() { }
    protected virtual void OnHoverExit() { }
    protected abstract void OnClick(PointerEventData eventData);

    private void ExitHover()
    {
        IsHovered = false;
        OnHoverExit();
        HoverExited?.Invoke(this);
    }

    protected virtual void OnDisable()
    {
        if(IsHovered) ExitHover();
    }
}
