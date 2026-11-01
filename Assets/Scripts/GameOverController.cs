using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverController : MonoBehaviour
{
    [SerializeField] private MonitorController _monitor;
    [SerializeField] private GameObject _jumpscareView;
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private float _jumpscareDuration = 1.5f;

    [Header("Vida")]
    [SerializeField] private Health _health;

    [Header("Som")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _jumpscareClip;

    private bool _triggered;
    private AudioSource _deathSource;

    private void Awake()
    {
        // Fonte própria só para o som de morte. Ignora a pausa do AudioListener,
        // por isso é o único som que se ouve depois de o jogador morrer.
        _deathSource = gameObject.AddComponent<AudioSource>();
        _deathSource.playOnAwake = false;
        _deathSource.spatialBlend = 0f;
        _deathSource.ignoreListenerPause = true;
    }

    private void OnEnable()
    {
        Monster.OnPlayerAttacked += HandleAttack;
        if (_health != null) _health.Died += HandleAttack;
    }

    private void OnDisable()
    {
        Monster.OnPlayerAttacked -= HandleAttack;
        if (_health != null) _health.Died -= HandleAttack;
    }

    private void Start()
    {
        AudioListener.pause = false;
        Time.timeScale = 1f;
        if (_jumpscareView != null) _jumpscareView.SetActive(false);
        if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        AudioListener.pause = false;
        Time.timeScale = 1f;
    }

    public void Restart()
    {
        AudioListener.pause = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void TriggerGameOver() => HandleAttack();

    private void HandleAttack()
    {
        if (_triggered) return;
        _triggered = true;

        if (_monitor != null) _monitor.ForceClose();
        Time.timeScale = 0f;

        // Corta todos os sons que estão a tocar e impede novos (portas, monitor, câmaras, ruído...).
        // Só a fonte de morte ignora esta pausa.
        AudioListener.pause = true;

        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        if (_jumpscareView != null) _jumpscareView.SetActive(true);
        if (_deathSource != null && _jumpscareClip != null) _deathSource.PlayOneShot(_jumpscareClip);

        yield return new WaitForSecondsRealtime(_jumpscareDuration);

        if (_jumpscareView != null) _jumpscareView.SetActive(false);
        if (_gameOverPanel != null) _gameOverPanel.SetActive(true);
    }
}