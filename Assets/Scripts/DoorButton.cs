using UnityEngine;
using UnityEngine.EventSystems;

public class DoorButton : Interactable
{
    [SerializeField] private Door _door;

    protected override void OnClick(PointerEventData eventData)
    {
        if (_door != null) _door.ToggleDoor();
    }
}
