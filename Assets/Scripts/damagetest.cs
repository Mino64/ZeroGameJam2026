using UnityEngine;

public class damagetest : Event
{
    [SerializeField] private int damage;
    [SerializeField] private Health health;
    public override void DoSomething()
    {
        health.DealDamage(damage);
    }
}
 