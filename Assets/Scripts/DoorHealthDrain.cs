using UnityEngine;

public class DoorHealthDrain : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Door[] doors;

    [Header("Dano (vida por segundo)")]
    [Tooltip("Dano com apenas uma porta fechada.")]
    [SerializeField] private float damagePerSecondOneDoor = 3f;
    [Tooltip("Dano com todas as portas fechadas. Com 3 ou mais portas, o dano intermédio é interpolado.")]
    [SerializeField] private float damagePerSecondAllDoors = 10f;

    private float accumulated;

    private void Start()
    {
        if (doors == null || doors.Length == 0)
        {
#if UNITY_2023_1_OR_NEWER
            doors = FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            doors = FindObjectsOfType<Door>(true);
#endif
        }
    }

    private void Update()
    {
        if (health == null || health.IsDead || doors == null) return;

        int closed = 0;
        foreach (Door d in doors)
            if (d != null && d.Closed) closed++;

        if (closed == 0)
        {
            accumulated = 0f;
            return;
        }

        accumulated += RateFor(closed) * Time.deltaTime;

        int damage = Mathf.FloorToInt(accumulated);
        if (damage <= 0) return;

        accumulated -= damage;
        health.DealDamage(damage);
    }

    private float RateFor(int closed)
    {
        if (doors.Length <= 1) return damagePerSecondAllDoors;

        float t = Mathf.Clamp01((closed - 1f) / (doors.Length - 1f));
        return Mathf.Lerp(damagePerSecondOneDoor, damagePerSecondAllDoors, t);
    }
}