using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MonitorController : MonoBehaviour
{
    [Header("Vistas")]
    [SerializeField] private GameObject cameraPanel;
    [SerializeField] private Room officeRoom;
    [SerializeField] private SpriteRenderer officeView;
    [SerializeField] private GameObject officeExtras;
    [SerializeField] private GameObject officeRoot;

    [Header("Controlos")]
    [SerializeField] private PentagramQTE qte;

    [SerializeField] private string toggleActionName = "Player/Jump";

    [Header("Transição ao abrir (office 1 -> office 2 -> câmaras)")]
    [Tooltip("Imagem (UI) que cobre o ecrã durante a transição. Sem imagem 1, não há transição.")]
    [SerializeField] private Image transitionImage;
    [SerializeField] private Sprite transitionSpriteA;
    [SerializeField] private Sprite transitionSpriteB;
    [Tooltip("Tempo total em segundos. A imagem 2 aparece a meio; no fim aparecem as câmaras.")]
    [SerializeField] private float transitionDuration = 3f;

    [Header("Som")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;

#if ENABLE_INPUT_SYSTEM
    private InputAction toggleAction;
#endif

    public bool IsOpen { get; private set; }

    public bool Locked { get; set; }

    // true enquanto as imagens de office da transição estão a ser mostradas (câmaras ainda escondidas)
    public bool InTransition { get; private set; }

    private Coroutine transitionRoutine;

    private bool HasTransition => transitionImage != null && transitionSpriteA != null && transitionDuration > 0f;

    public event Action<bool> Toggled;

    private void OnEnable()
    {
        if (officeRoom) officeRoom.Changed += RefreshOffice;

#if ENABLE_INPUT_SYSTEM
        toggleAction = InputSystem.actions != null
            ? InputSystem.actions.FindAction(toggleActionName)
            : null;

        if (toggleAction == null)
            Debug.LogWarning($"[MonitorController] A��o '{toggleActionName}' n�o encontrada no " +
                             "project-wide actions asset. O monitor s� abre pelo bot�o.");
#endif
    }

    private void OnDisable()
    {
        if (officeRoom) officeRoom.Changed -= RefreshOffice;
    }

    private void Start()
    {
        RefreshOffice();
        Apply(false);
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (toggleAction != null && toggleAction.WasPressedThisFrame()) Toggle();
#else
            if (Input.GetKeyDown(KeyCode.Space)) Toggle();
#endif
    }
    public void Toggle() => SetOpen(!IsOpen);

    public void SetOpen(bool open)
    {
        if (open == IsOpen || Locked) return;

        if (!open && qte != null)
        {
            if (qte.IsFinishing) return;
            qte.Cancel();
        }

        Apply(open);
        Toggled?.Invoke(open);

        AudioClip clip = open ? openClip : closeClip;
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    public void ForceClose()
    {
        Locked = true;
        if (!IsOpen) return;

        Apply(false);
        Toggled?.Invoke(false);
    }

    private void Apply(bool open)
    {
        IsOpen = open;

        StopTransition();
        bool transition = open && HasTransition;

        cameraPanel.SetActive(open && !transition);
        if (officeRoot != null) officeRoot.SetActive(!open);
        else if (officeView != null) officeView.gameObject.SetActive(!open);
        if (officeExtras != null) officeExtras.SetActive(!open);

        if (transition) transitionRoutine = StartCoroutine(RunTransition());
    }

    private IEnumerator RunTransition()
    {
        InTransition = true;

        transitionImage.sprite = transitionSpriteA;
        transitionImage.gameObject.SetActive(true);

        float half = transitionDuration * 0.5f;

        yield return new WaitForSeconds(half);

        if (transitionSpriteB != null) transitionImage.sprite = transitionSpriteB;

        yield return new WaitForSeconds(half);

        transitionImage.gameObject.SetActive(false);
        InTransition = false;
        transitionRoutine = null;

        if (IsOpen) cameraPanel.SetActive(true);
    }

    private void StopTransition()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        InTransition = false;
        if (transitionImage != null) transitionImage.gameObject.SetActive(false);
    }
    private void RefreshOffice()
    {
        if (officeRoom && officeView) officeView.sprite = officeRoom.GetCurrentSprite();
    }
}