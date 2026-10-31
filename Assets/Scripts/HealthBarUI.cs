using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("Fill")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Gradient colors = CreateDefaultGradient();
    [SerializeField] private float fillSpeed = 1.5f;

    [Header("Pop animation")]
    [SerializeField] private AnimationCurve scaleParams = AnimationCurve.EaseInOut(0f, 1.2f, 1f, 1f);
    [SerializeField] private float scaleDuration = 0.3f;

    [SerializeField] private Health player;
    private RectTransform rectTransform;
    private int prevHealth;
    private float timeSinceLastChange;
    private float displayedFill;
    private bool initialized;

    private static Gradient CreateDefaultGradient()
    {
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.85f, 0.1f, 0.1f), 0f),
                new GradientColorKey(new Color(0.95f, 0.8f, 0.1f), 0.5f),
                new GradientColorKey(new Color(0.2f, 0.85f, 0.2f), 1f),
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return g;
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        timeSinceLastChange = scaleDuration;
    }

    private void Update()
    {
        if (player == null || fillImage == null) return;

        if (!initialized)
        {
            initialized = true;
            prevHealth = player.HP;
            displayedFill = GetTargetFill();
            ApplyFill();
        }

        int health = player.HP;

        if (health != prevHealth)
        {
            timeSinceLastChange = 0f;
            prevHealth = health;
        }

        UpdatePop();
        UpdateFill();
    }

    private void UpdatePop()
    {
        float t = 1f;
        if (scaleDuration > 0f && timeSinceLastChange < scaleDuration)
        {
            timeSinceLastChange += Time.deltaTime;
            t = Mathf.Clamp01(timeSinceLastChange / scaleDuration);
        }

        float x = scaleParams.Evaluate(t);
        rectTransform.localScale = new Vector3(x, x, 1f);
    }

    private void UpdateFill()
    {
        float target = GetTargetFill();

        displayedFill = Mathf.MoveTowards(displayedFill, target, fillSpeed * Time.deltaTime);

        ApplyFill();
    }

    private float GetTargetFill()
    {
        if (player == null || player.MaxHealth <= 0) return 0f;
        return Mathf.Clamp01(player.HP / (float)player.MaxHealth);
    }

    private void ApplyFill()
    {
        fillImage.fillAmount = displayedFill;
        fillImage.color = colors.Evaluate(displayedFill);
    }
}