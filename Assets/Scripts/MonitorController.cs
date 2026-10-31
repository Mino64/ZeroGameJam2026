using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MonitorController : MonoBehaviour
{
    [Header("Vistas")]
    [SerializeField] private GameObject cameraPanel;
    [SerializeField] private Room officeRoom;
    [SerializeField] private SpriteRenderer officeView;

    [Header("Controlos")]
    [SerializeField] private PentagramQTE qte;

    [SerializeField] private string toggleActionName = "Player/Jump";

    [Header("Som")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;

#if ENABLE_INPUT_SYSTEM
    private InputAction toggleAction;
#endif

    public bool IsOpen { get; private set; }

    public bool Locked { get; set; }

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
        cameraPanel.SetActive(open);
        officeView.gameObject.SetActive(!open);
    }
    private void RefreshOffice()
    {
        if (officeRoom && officeView) officeView.sprite = officeRoom.GetCurrentSprite();
    }
}