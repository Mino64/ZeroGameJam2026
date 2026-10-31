using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverController : MonoBehaviour
{
    [SerializeField] private MonitorController _monitor;
    [SerializeField] private GameObject _jumpscareView;
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private float _jumpscareDuration = 1.5f;

    [Header("Som")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _jumpscareClip;

    private bool _triggered;

    private void OnEnable() => Monster.OnPlayerAttacked += HandleAttack;
    private void OnDisable() => Monster.OnPlayerAttacked -= HandleAttack;

    private void Start()
    {
        Time.timeScale = 1f;
        if (_jumpscareView != null) _jumpscareView.SetActive(false);
        if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
    }

    private void OnDestroy() => Time.timeScale = 1f;

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void HandleAttack()
    {
        if (_triggered) return;
        _triggered = true;

        if (_monitor != null) _monitor.ForceClose();
        Time.timeScale = 0f;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        if (_jumpscareView != null) _jumpscareView.SetActive(true);
        if (_audioSource != null && _jumpscareClip != null) _audioSource.PlayOneShot(_jumpscareClip);

        yield return new WaitForSecondsRealtime(_jumpscareDuration);

        if (_jumpscareView != null) _jumpscareView.SetActive(false);
        if (_gameOverPanel != null) _gameOverPanel.SetActive(true);
    }
}
