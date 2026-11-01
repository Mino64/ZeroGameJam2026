using Unity.VisualScripting;
using UnityEngine;
public class FreddleBehaviour : MonoBehaviour
{
    [Header("Valores de timers")]
    [SerializeField] private float MinSafeValue;
    [SerializeField] private float MaxSafeValue;

    [SerializeField] private float MinWarningValue;
    [SerializeField] private float MaxWarningValue;

    [SerializeField] private float MinKillValue;
    [SerializeField] private float MaxKillValue;

    
    [Header("Visual")]
    [SerializeField] private Sprite SafeIMG;
    [SerializeField] private Sprite WarnIMG;
    [SerializeField] private Sprite KillIMG;
    [SerializeField] private SpriteRenderer SpriteRenderer;

    double SafeValue;
    double WarningValue;
    double KillValue;

    enum States
    {
        Safe,
        Warning, 
        Kill
    }

    States state = States.Safe;

    private void Start()
    {

        SafeValue = Random.Range(MinSafeValue, MaxSafeValue);
        WarningValue = Random.Range(MinWarningValue, MaxWarningValue);
        KillValue = Random.Range(MinKillValue, MaxKillValue);

        Debug.Log($"Safe: {SafeValue} Warning: {WarningValue} Kill: {KillValue}");
    }

    float Timer;

    private void Update()
    {
        Timer += Time.deltaTime;
        switch (state)
        {
            case States.Safe:
                SpriteRenderer.sprite=SafeIMG;
                if (Timer> SafeValue)
                {
                    Timer = 0;
                    state = States.Warning;
                }
                break;
            case States.Warning:
                SpriteRenderer.sprite = WarnIMG;
                if (Timer > WarningValue)
                {
                    Timer = 0;
                    state = States.Kill;
                }
                break;
            case States.Kill:
                SpriteRenderer.sprite = KillIMG;
                if (Timer > KillValue)
                {
                    Timer = 0;
                    Debug.Log("BAH! You dead");
                }
                break;
            }
    }
    private void OnBecameVisible()
    {
        Debug.Log("Ah! You got me!");
        Timer = 0;
        Invoke(nameof(ResetState),2);
    }

    private void ResetState()
    {
        state=States.Safe;
    }
}
