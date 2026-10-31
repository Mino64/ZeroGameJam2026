using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CameraInterference : MonoBehaviour
{
    [Header("Overlay")]
    [SerializeField] private RawImage staticImage;

    [Header("Efeito")]
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private Vector2 alphaRange = new Vector2(0.25f, 0.9f);
    [SerializeField] private Vector2 tiling = new Vector2(4f, 3f);

    [Header("Tremor")]
    [SerializeField] private RectTransform shakeTarget;
    [SerializeField] private float shakeStrength = 6f;

    [Header("Som")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip staticClip;

    private CanvasGroup group;
    private Coroutine routine;
    private Vector2 shakeOrigin;

    private void Awake()
    {
        if (staticImage == null)
        {
            Debug.LogError("[CameraInterference] Falta atribuir a RawImage 'Static Image'.", this);
            enabled = false;
            return;
        }

        group = staticImage.GetComponent<CanvasGroup>();
        if (group == null) group = staticImage.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        staticImage.raycastTarget = false;

        if (staticImage.texture == null) staticImage.texture = MakeNoiseTexture();

        if (shakeTarget != null) shakeOrigin = shakeTarget.anchoredPosition;
    }

    private void OnEnable() => Monster.OnMonsterMoved += Play;

    private void OnDisable()
    {
        Monster.OnMonsterMoved -= Play;
        if (routine != null) { StopCoroutine(routine); routine = null; }
        ResetVisuals();
    }

    public void Play()
    {
        if (!isActiveAndEnabled) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run());

        if (audioSource != null && staticClip != null) audioSource.PlayOneShot(staticClip);
    }

    private IEnumerator Run()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / duration);

            group.alpha = Random.Range(alphaRange.x, alphaRange.y) * k;
            staticImage.uvRect = new Rect(Random.value, Random.value, tiling.x, tiling.y);

            if (shakeTarget != null)
                shakeTarget.anchoredPosition = shakeOrigin + Random.insideUnitCircle * shakeStrength * k;

            yield return null;
        }

        routine = null;
        ResetVisuals();
    }

    private void ResetVisuals()
    {
        if (group != null) group.alpha = 0f;
        if (shakeTarget != null) shakeTarget.anchoredPosition = shakeOrigin;
    }

    private static Texture2D MakeNoiseTexture(int size = 128)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Point
        };

        var pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            byte v = (byte)Random.Range(0, 256);
            pixels[i] = new Color32(v, v, v, 255);
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }
}