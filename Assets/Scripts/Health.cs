using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    public int MaxHealth {  get { return maxHealth; } }
    public int HP {get; set;}
    public bool IsDead{get; private set;}


    private void Start()
    {
        HP = maxHealth;
    }

    public void DealDamage(int value)
    {
        if(IsDead) return;
    
        HP = HP - value;
        Debug.Log($"{HP}");

        if(HP <=0)
        {
            IsDead = true;
            Debug.Log("UH oh u died");
        }
    }
}
