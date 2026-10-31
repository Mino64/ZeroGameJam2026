using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Fnaf7.Assets.Scripts
{
    public class testInteractable : Interactable
    {
        [SerializeField] private UnityEvent onHoverEnter;
        [SerializeField] private UnityEvent onHoverExit;
        [SerializeField] private UnityEvent onClick;

    protected override void OnHoverEnter()
    {
        Debug.Log($"Hover Enter");
        onHoverEnter?.Invoke();
    }

    protected override void OnHoverExit()
    {
        Debug.Log($"Hover Exit");
        onHoverExit?.Invoke();
    }

    protected override void OnClick(PointerEventData eventData)
    {
        Debug.Log($"Click ({eventData.button})");
        onClick?.Invoke();
    }
    }
}