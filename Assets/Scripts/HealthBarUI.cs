using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("Fill")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Gradient colors = CreateDefaultGradient();
    [SerializeField] private float fillSpeed = 1.5f;


    [SerializeField] private Health player;
    private RectTransform rectTransform;
    private int prevHealth;
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
            prevHealth = health;
        }


        UpdateFill();
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