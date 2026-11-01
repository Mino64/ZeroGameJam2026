using UnityEngine;
using UnityEngine.EventSystems;

public class MonitorHoverZone : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private MonitorController _monitor;
    [SerializeField] private float _cooldown = 0.5f;
    [Tooltip("Bloqueia TODAS as zonas de hover durante este tempo depois de esta agir.")]
    [SerializeField] private float _sharedCooldown = 0.3f;

    private float _nextAllowedTime;

    // Ao (re)aparecer, espera um instante: evita agir logo se ficar debaixo do rato
    private void OnEnable() => HoverZoneCooldown.Block(_sharedCooldown);

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_monitor == null || !HoverZoneCooldown.Ready || Time.unscaledTime < _nextAllowedTime) return;

        _nextAllowedTime = Time.unscaledTime + _cooldown;
        HoverZoneCooldown.Block(_sharedCooldown);
        _monitor.Toggle();
    }
}