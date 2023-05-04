using UnityEngine;

public class DamageListener : MonoBehaviour
{
    private DamageBehavior damageBehavior;

    private void Start()
    {
        damageBehavior = GetComponentInChildren<DamageBehavior>();
    }

    public void StartDealingDamage()
        => damageBehavior?.StartDealingDamage();
        
    public void StopDealingDamage()
        => damageBehavior?.StopDealingDamage();

}
