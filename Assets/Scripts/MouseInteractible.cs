using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
public class testInteractable : Interactable
{
        [SerializeField] private UnityEvent onHoverEnter;
        [SerializeField] private UnityEvent onHoverExit;
        [SerializeField] private UnityEvent onClick;
        protected override void OnClick(PointerEventData eventData)
        {
            onClick?.Invoke();
        }
}