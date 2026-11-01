using UnityEngine;
using UnityEngine.EventSystems;

public class OfficeBackZone : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private OfficeViewController _office;
    [SerializeField] private float _cooldown = 0.5f;

    private float _nextAllowedTime;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_office == null || Time.timeScale == 0f || Time.unscaledTime < _nextAllowedTime) return;

        _nextAllowedTime = Time.unscaledTime + _cooldown;
        _office.ToggleBack();
    }
}
