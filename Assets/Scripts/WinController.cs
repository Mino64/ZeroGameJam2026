using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinController : MonoBehaviour
{
    [SerializeField] private RepairController _repair;
    [SerializeField] private MonitorController _monitor;
    [SerializeField] private GameObject _winPanel;
    [SerializeField] private AlterableRoom[] _rooms;

    [Header("Menu principal")]
    [Tooltip("Índice da cena do menu principal nas Build Settings.")]
    [SerializeField] private int _mainMenuScene = 0;

    [Header("Som")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _winClip;

    private readonly HashSet<AlterableRoom> _repaired = new HashSet<AlterableRoom>();
    private bool _won;

    public int RepairedCount => _repaired.Count;
    public int RoomCount => _rooms != null ? _rooms.Length : 0;

    private void Start()
    {
        Time.timeScale = 1f;
        if (_winPanel != null) _winPanel.SetActive(false);

        if (_rooms == null || _rooms.Length == 0)
        {
#if UNITY_2023_1_OR_NEWER
            _rooms = FindObjectsByType<AlterableRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            _rooms = FindObjectsOfType<AlterableRoom>(true);
#endif
        }
    }

    private void OnEnable()
    {
        if (_repair != null) _repair.Repaired += HandleRepaired;
    }

    private void OnDisable()
    {
        if (_repair != null) _repair.Repaired -= HandleRepaired;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(_mainMenuScene);
    }

    private void HandleRepaired(AlterableRoom room)
    {
        if (_won || room == null) return;

        if (Time.timeScale == 0f) return;

        _repaired.Add(room);

        if (_rooms == null || _rooms.Length == 0) return;

        foreach (AlterableRoom r in _rooms)
            if (r != null && !_repaired.Contains(r)) return;

        Win();
    }

    private void Win()
    {
        _won = true;

        if (_monitor != null) _monitor.ForceClose();
        if (_audioSource != null && _winClip != null) _audioSource.PlayOneShot(_winClip);
        if (_winPanel != null) _winPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    private void OnDestroy() => Time.timeScale = 1f;
}