using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI_Bar : MonoBehaviour
{
    [Header("Fill")]
    [SerializeField] private Image fillImage;      // Image Type = Filled, Horizontal, Origin Left
    [SerializeField] private Gradient colors;
    [SerializeField] private float fillSpeed = 1.5f; // fill units per second

    [Header("Pop animation")]
    [SerializeField] private AnimationCurve scaleParams = AnimationCurve.EaseInOut(0f, 1.2f, 1f, 1f);
    [SerializeField] private float scaleDuration = 0.3f;

    [SerializeField] private Health player;
    private RectTransform rectTransform;
    private int prevHealth;
    private float timeSinceLastChange;
    private float displayedFill;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        timeSinceLastChange = scaleDuration; // don't pop on startup
    }

    private void Update()
    {
        if (player == null)
        {
            if (player == null) return;

            // Snap to the player's current state, no animation
            prevHealth = player.HP;
            displayedFill = GetTargetFill();
            ApplyFill();
        }

        int health = player.HP;

        // Detect change -> restart pop animation
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

        // Frame-rate independent, constant-speed smoothing
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

        // If you prefer scaling instead of Image.Type = Filled:
        // fillImage.rectTransform.localScale = new Vector3(displayedFill, 1f, 1f);
    }
}