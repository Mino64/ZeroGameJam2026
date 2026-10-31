using UnityEngine;
using UnityEngine.EventSystems;

public class MonitorHoverZone : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private MonitorController _monitor;
    [SerializeField] private float _cooldown = 0.5f;

    private float _nextAllowedTime;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_monitor == null || Time.unscaledTime < _nextAllowedTime) return;

        _nextAllowedTime = Time.unscaledTime + _cooldown;
        _monitor.Toggle();
    }
}