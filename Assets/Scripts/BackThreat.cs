using System;
using UnityEngine;

public class BackThreat : MonoBehaviour
{
    public enum ThreatState { Safe = 0, Dangerous = 1, Kill = 2 }

    [Header("Ecr�")]
    [SerializeField] private SpriteRenderer screen;
    [SerializeField] private Sprite safeSprite;
    [SerializeField] private Sprite dangerousSprite;
    [SerializeField] private Sprite killSprite;

    [Header("Tempo em cada estado (segundos, sorteado entre min e max)")]
    [SerializeField] private Vector2 safeTime = new Vector2(30f, 50f);
    [SerializeField] private Vector2 dangerousTime = new Vector2(8f, 14f);

    [Header("Recuperar")]
    [SerializeField] private float lookTimeToRecover = 2f;

    [Header("Refer�ncias")]
    [SerializeField] private OfficeViewController office;
    [SerializeField] private GameOverController gameOver;

    [Header("Som (2D: ouve-se em qualquer vista, mesmo com o monitor aberto)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip safeClip;
    [SerializeField] private AudioClip dangerousClip;
    [SerializeField] private AudioClip killClip;
    [SerializeField, Range(0f, 1f)] private float backVolume = 1f;
    [Tooltip("Frequência de corte do filtro (Hz). Mais baixo = mais abafado = soa mais a vir de trás.")]
    [SerializeField, Range(500f, 22000f)] private float muffleCutoff = 2500f;

    public ThreatState State { get; private set; } = ThreatState.Safe;

    public event Action<ThreatState> StateChanged;

    private float stateTimer;
    private float lookTimer;
    private bool dead;

    private Transform soundOrigin;

    private void Awake()
    {
        if (audioSource != null) return;   // fonte atribuída à mão: usa as definições dela

        // Fonte própria, num objeto filho (para o filtro não abafar os outros sons do GameManager)
        var go = new GameObject("BackThreatSound");
        go.transform.SetParent(transform, false);
        soundOrigin = go.transform;

        audioSource = go.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;                    // 3D: posicionado atrás do jogador
        audioSource.dopplerLevel = 0f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 50f;                    // sem perda de volume com a distância
        audioSource.maxDistance = 100f;
        audioSource.volume = backVolume;

        // Som abafado: dá a sensação de vir de trás (em colunas stereo não há "trás" real)
        var lp = go.AddComponent<AudioLowPassFilter>();
        lp.cutoffFrequency = muffleCutoff;
    }

    // Coloca a fonte atrás do ouvinte (a câmara olha para +Z, logo atrás é -Z)
    private void PlaceBehindListener()
    {
        if (soundOrigin == null) return;

        var listener = FindFirstObjectByType<AudioListener>();
        Transform t = listener != null ? listener.transform : null;
        soundOrigin.position = t != null ? t.position - t.forward * 5f : new Vector3(0f, 0f, -15f);
    }

    private void Start() => SetState(ThreatState.Safe, false);

    private void Update()
    {
        if (dead || Time.timeScale == 0f) return;

        bool looking = office != null && office.IsLooking(OfficeView.Back);

        if (looking)
        {
            if (State == ThreatState.Safe)
            {
                lookTimer = 0f;
                return;
            }

            lookTimer += Time.deltaTime;
            if (lookTimer >= lookTimeToRecover) SetState(State - 1);
            return;
        }

        lookTimer = 0f;

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f) SetState(State + 1);
    }

    private void SetState(ThreatState state, bool playSound = true)
    {
        State = state;
        lookTimer = 0f;

        if (playSound && audioSource != null)
        {
            AudioClip clip = state == ThreatState.Safe ? safeClip
                           : state == ThreatState.Dangerous ? dangerousClip
                           : killClip;
            if (clip != null)
            {
                PlaceBehindListener();
                audioSource.PlayOneShot(clip);
            }
        }

        if (screen != null)
        {
            Sprite sprite = state == ThreatState.Safe ? safeSprite
                          : state == ThreatState.Dangerous ? dangerousSprite
                          : killSprite;
            if (sprite != null) screen.sprite = sprite;
        }

        switch (state)
        {
            case ThreatState.Safe: stateTimer = UnityEngine.Random.Range(safeTime.x, safeTime.y); break;
            case ThreatState.Dangerous: stateTimer = UnityEngine.Random.Range(dangerousTime.x, dangerousTime.y); break;
        }

        StateChanged?.Invoke(state);

        if (state == ThreatState.Kill)
        {
            dead = true;
            if (gameOver != null) gameOver.TriggerGameOver();
        }
    }
}