using UnityEngine;

public class OfficeEntrance : MonoBehaviour
{
    [SerializeField] private Room _fromRoom;
    [SerializeField] private Door _door;

    [Header("Som de bater")]
    [SerializeField] private AudioSource _knockSource;
    [SerializeField] private AudioClip[] _knockClips;
    [SerializeField, Range(-1f, 1f)] private float _pan;

    private int _lastKnockIndex = -1;

    public Room FromRoom => _fromRoom;
    public bool IsClosed => _door != null && _door.Closed;

    public void Knock()
    {
        if (_knockSource == null || _knockClips == null || _knockClips.Length == 0) return;

        int i = Random.Range(0, _knockClips.Length);
        if (_knockClips.Length > 1)
            while (i == _lastKnockIndex) i = Random.Range(0, _knockClips.Length);
        _lastKnockIndex = i;

        if (_knockClips[i] == null) return;

        if (!_knockSource.isActiveAndEnabled)
        {
            Debug.LogWarning("[OfficeEntrance] O AudioSource do som de bater esta desativado. " +
                             "Poe-no num objeto fora da porta (as portas desativam com o monitor).", this);
            return;
        }

        _knockSource.spatialBlend = 0f;
        _knockSource.panStereo = _pan;
        _knockSource.PlayOneShot(_knockClips[i]);
    }
}