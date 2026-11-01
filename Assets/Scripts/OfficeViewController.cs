using System;
using UnityEngine;

public class OfficeViewController : MonoBehaviour
{
    [Header("Vistas (cada uma � um objeto com a sua imagem e os seus objetos, como as portas)")]
    [SerializeField] private GameObject frontView;
    [SerializeField] private GameObject rightView;
    [SerializeField] private GameObject backView;
    [SerializeField] private GameObject leftView;

    [Header("Controlos")]
    [SerializeField] private MonitorController monitor;
    [SerializeField] private GameObject turnZones;
    [SerializeField] private GameObject[] sideZones;
    [SerializeField] private OfficeView startView = OfficeView.Front;

    [Header("Som")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip turnClip;

    public OfficeView Current { get; private set; }

    public bool MonitorOpen => monitor != null && monitor.IsOpen;

    public bool IsLooking(OfficeView view) => !MonitorOpen && Current == view;

    public event Action<OfficeView> ViewChanged;

    private OfficeView returnView = OfficeView.Front;

    private void OnEnable()
    {
        if (monitor != null) monitor.Toggled += OnMonitorToggled;
    }

    private void OnDisable()
    {
        if (monitor != null) monitor.Toggled -= OnMonitorToggled;
    }

    private void Start() => Show(startView, false);

    private void Update()
    {
        if (Time.timeScale == 0f || MonitorOpen) return;

#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;

        if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) Turn(-1);
        if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) Turn(1);
        if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) ToggleBack();
#else
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) Turn(-1);
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) Turn(1);
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) ToggleBack();
#endif
    }

    public void Turn(int direction)
    {
        if (direction == 0 || MonitorOpen || Current == OfficeView.Back) return;

        int pos = Current == OfficeView.Left ? -1 : Current == OfficeView.Right ? 1 : 0;
        pos = Mathf.Clamp(pos + Math.Sign(direction), -1, 1);

        Show(pos < 0 ? OfficeView.Left : pos > 0 ? OfficeView.Right : OfficeView.Front);
    }

    public void ToggleBack()
    {
        if (MonitorOpen) return;

        if (Current == OfficeView.Back)
        {
            Show(returnView);
        }
        else
        {
            returnView = Current;
            Show(OfficeView.Back);
        }
    }

    public void Show(OfficeView view, bool playSound = true)
    {
        bool changed = view != Current;
        Current = view;

        SetActive(frontView, view == OfficeView.Front);
        SetActive(rightView, view == OfficeView.Right);
        SetActive(backView, view == OfficeView.Back);
        SetActive(leftView, view == OfficeView.Left);

        RefreshZones();

        if (!changed) return;

        if (playSound && audioSource != null && turnClip != null) audioSource.PlayOneShot(turnClip);
        ViewChanged?.Invoke(view);
    }

    private void OnMonitorToggled(bool open) => RefreshZones();

    private void RefreshZones()
    {
        if (turnZones != null) turnZones.SetActive(!MonitorOpen);

        if (sideZones == null) return;

        // sideZones[0] = esquerda, sideZones[1] = direita.
        // Esconde a seta do lado para onde já estamos a olhar (e ambas quando estamos de costas).
        for (int i = 0; i < sideZones.Length; i++)
        {
            if (sideZones[i] == null) continue;

            bool visible = Current != OfficeView.Back
                           && !(i == 0 && Current == OfficeView.Left)
                           && !(i == 1 && Current == OfficeView.Right);
            sideZones[i].SetActive(visible);
        }
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active) go.SetActive(active);
    }
}