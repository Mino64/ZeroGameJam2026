using UnityEngine;
using UnityEngine.EventSystems;

public class OfficeTurnZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private OfficeViewController _office;
    [Tooltip("-1 = esquerda, 1 = direita")]
    [SerializeField] private int _direction = 1;
    [SerializeField] private float _repeatDelay = 0.8f;

    private bool _hovered;
    private float _nextAllowedTime;

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
        TryTurn();
    }

    public void OnPointerExit(PointerEventData eventData) => _hovered = false;

    private void OnDisable() => _hovered = false;

    private void Update()
    {
        if (_hovered) TryTurn();
    }

    private void TryTurn()
    {
        if (_office == null || Time.timeScale == 0f || Time.unscaledTime < _nextAllowedTime) return;

        _nextAllowedTime = Time.unscaledTime + _repeatDelay;
        _office.Turn(_direction);
    }
}