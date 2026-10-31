using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PentagramQTE : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI")]
    [SerializeField] private RectTransform area;
    [SerializeField] private RectTransform circle;
    [SerializeField] private RectTransform nodePrefab;
    [SerializeField] private Image linePrefab;
    [SerializeField] private Image timerBar;
    [SerializeField] private CanvasGroup staticOverlay;
    [SerializeField] private Camera uiCamera;

    [Header("Forma")]
    [SerializeField] private float radius = 150f;
    [SerializeField] private float circlePadding = 0f;
    [SerializeField] private float startAngle = -90f;
    [SerializeField] private float lineThickness = 6f;
    [SerializeField] private bool showGuide = true;

    [Header("Jogabilidade")]
    [SerializeField] private float hitRadius = 35f;
    [SerializeField] private float timeLimit = 4f;

    [Header("Feedback")]
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color litColor = new Color(1f, 0.25f, 0.25f);
    [SerializeField] private Color lineColor = new Color(1f, 0.25f, 0.25f);
    [SerializeField] private Color guideColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private Color failColor = new Color(0.6f, 0f, 0f);
    [SerializeField] private Color successColor = new Color(0.4f, 1f, 0.4f);
    [SerializeField] private float successFeedbackTime = 0.35f;
    [SerializeField] private float failFeedbackTime = 0.5f;
    [SerializeField] private float shakeStrength = 18f;

    // O AudioSource nao pode estar neste objeto: ele e desativado no fim do QTE e cortava o som
    [Header("Som")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip failClip;

    private static readonly int[] Order = { 0, 2, 4, 1, 3, 0 };

    public bool IsActive { get; private set; }
    public bool IsFinishing => finishing;

    private readonly List<RectTransform> nodes = new List<RectTransform>();
    private readonly List<Image> nodeImages = new List<Image>();
    private readonly List<Image> lines = new List<Image>();
    private readonly List<Image> guideLines = new List<Image>();
    private Image tail;
    private System.Action onSuccess, onFail;
    private float timer;
    private float currentLimit;
    private bool dragging;
    private bool finishing;
    private int step;
    private Vector2 areaOrigin;

    [SerializeField] private string pointActionName = "UI/Point";
#if ENABLE_INPUT_SYSTEM
    private InputAction pointAction;
#endif

    public void Begin(System.Action success, System.Action fail, float? customTime = null)
    {
        onSuccess = success;
        onFail = fail;
        currentLimit = customTime ?? timeLimit;
        timer = currentLimit;
        IsActive = true;
        finishing = false;
        gameObject.SetActive(true);

        areaOrigin = area.anchoredPosition;
        if (staticOverlay) staticOverlay.alpha = 0f;

        BuildNodes();
        ResetProgress();
    }

    public void Cancel()
    {
        if (!IsActive || finishing) return;
        Close();
    }

    private void BuildNodes()
    {
        ClearAll();

        if (circle)
        {
            circle.anchoredPosition = Vector2.zero;
            circle.sizeDelta = Vector2.one * (radius + circlePadding) * 2f;
        }

        for (int i = 0; i < 5; i++)
        {
            float angle = (startAngle + i * 72f) * Mathf.Deg2Rad;
            RectTransform n = Instantiate(nodePrefab, area);
            n.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            Image img = n.GetComponent<Image>();
            if (img) { img.color = idleColor; img.raycastTarget = false; }

            nodes.Add(n);
            nodeImages.Add(img);
        }

        if (showGuide)
        {
            for (int i = 0; i < Order.Length - 1; i++)
            {
                Image g = Instantiate(linePrefab, area);
                g.raycastTarget = false;
                g.color = guideColor;
                SetLine(g, nodes[Order[i]].anchoredPosition, nodes[Order[i + 1]].anchoredPosition);
                g.transform.SetSiblingIndex(circle ? circle.GetSiblingIndex() + 1 : 0);
                guideLines.Add(g);
            }
        }

        tail = Instantiate(linePrefab, area);
        tail.raycastTarget = false;
        tail.color = lineColor;
        tail.gameObject.SetActive(false);

        if (staticOverlay) staticOverlay.transform.SetAsLastSibling();
    }

    private void Update()
    {
        if (!IsActive || finishing) return;

        timer -= Time.deltaTime;
        if (timerBar) timerBar.fillAmount = Mathf.Clamp01(timer / currentLimit);
        if (timer <= 0f) { Finish(false); return; }

        if (!dragging) return;

        Vector2 mouse = MouseLocal();
        Vector2 last = nodes[Order[step - 1]].anchoredPosition;
        SetLine(tail, last, mouse);

        Vector2 target = nodes[Order[step]].anchoredPosition;
        if (Vector2.Distance(mouse, target) <= hitRadius)
        {
            Image seg = Instantiate(linePrefab, area);
            seg.raycastTarget = false;
            seg.color = lineColor;
            SetLine(seg, last, target);
            lines.Add(seg);

            SetNodeColor(Order[step], litColor);
            step++;

            if (step >= Order.Length)
            {
                Finish(true);
                return;
            }

            if (staticOverlay) staticOverlay.transform.SetAsLastSibling();
        }
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!IsActive || finishing) return;

        if (Vector2.Distance(MouseLocal(), nodes[Order[0]].anchoredPosition) <= hitRadius)
        {
            dragging = true;
            step = 1;
            SetNodeColor(Order[0], litColor);
            tail.gameObject.SetActive(true);
        }
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!IsActive || finishing || !dragging) return;
        ResetProgress();
    }

    private void ResetProgress()
    {
        dragging = false;
        step = 0;

        foreach (Image l in lines) if (l) Destroy(l.gameObject);
        lines.Clear();

        foreach (Image img in nodeImages) if (img) img.color = idleColor;
        if (tail) tail.gameObject.SetActive(false);
    }

    private void Finish(bool success)
    {
        if (finishing) return;
        StartCoroutine(FinishRoutine(success));
    }

    private IEnumerator FinishRoutine(bool success)
    {
        finishing = true;
        dragging = false;
        if (tail) tail.gameObject.SetActive(false);

        System.Action callback = success ? onSuccess : onFail;

        if (success)
        {
            PaintAll(successColor);
            PlaySound(successClip);
            yield return new WaitForSeconds(successFeedbackTime);
        }
        else
        {
            PaintAll(failColor);
            PlaySound(failClip);
            yield return FailFeedback();
        }

        Close();
        callback?.Invoke();
    }
    private IEnumerator FailFeedback()
    {
        float t = 0f;
        while (t < failFeedbackTime)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / failFeedbackTime);

            area.anchoredPosition = areaOrigin + Random.insideUnitCircle * shakeStrength * k;
            if (staticOverlay) staticOverlay.alpha = Random.Range(0.4f, 1f) * k;

            yield return null;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    private void PaintAll(Color c)
    {
        foreach (Image img in nodeImages) if (img) img.color = c;
        foreach (Image l in lines) if (l) l.color = c;
    }

    private void SetNodeColor(int index, Color c)
    {
        if (nodeImages[index]) nodeImages[index].color = c;
    }

    private void Close()
    {
        IsActive = false;
        finishing = false;
        dragging = false;

        area.anchoredPosition = areaOrigin;
        if (staticOverlay) staticOverlay.alpha = 0f;

        ClearAll();
        gameObject.SetActive(false);
    }

    private void ClearAll()
    {
        foreach (RectTransform n in nodes) if (n) Destroy(n.gameObject);
        foreach (Image l in lines) if (l) Destroy(l.gameObject);
        foreach (Image g in guideLines) if (g) Destroy(g.gameObject);
        if (tail) Destroy(tail.gameObject);

        nodes.Clear();
        nodeImages.Clear();
        lines.Clear();
        guideLines.Clear();
        tail = null;
    }

    private Vector2 MouseLocal()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            area, PointerScreenPosition(), uiCamera, out var local);
        return local;
    }

    private Vector2 PointerScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (pointAction == null && InputSystem.actions != null)
            pointAction = InputSystem.actions.FindAction(pointActionName);

        if (pointAction != null) return pointAction.ReadValue<Vector2>();

        return Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
#else
            return Input.mousePosition;
#endif
    }

    private void SetLine(Image img, Vector2 a, Vector2 b)
    {
        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = a;
        Vector2 d = b - a;
        rt.sizeDelta = new Vector2(d.magnitude, lineThickness);
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
    }
}