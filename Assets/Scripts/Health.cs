using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;

    public int Health {get; set;}
    public bool IsDead{get; private set;}


    private void Start()
    {
        Health = maxHealth;
    }

    public void DealDamage(int value)
    {
        if(isDead) return;
    
        Health = Health - value;
        Debug.Log($"{Health}");

        if(Health <=0)
        {
            IsDead = true;
            Debug.Log("UH oh u died");
        }
    }
}
