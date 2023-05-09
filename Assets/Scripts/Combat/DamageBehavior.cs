using System.Collections.Generic;
using Health;
using NaughtyAttributes;
using UnityEngine;

public class DamageBehavior : MonoBehaviour
{
    [SerializeField] private float weaponDamage;
    [SerializeField, Tag] private string TagGO;

    private bool canDealDamage;
    private RaycastHit hit;
    private List<GameObject> damaged;
    
    public delegate void OnPlayerDamaged(GameObject player);
    public delegate void OnEnemyDamaged(GameObject player);
    public static OnPlayerDamaged onPlayerDamaged;
    public static OnEnemyDamaged onEnemyDamaged;

    private void Start()
        => damaged = new List<GameObject>();
    
    private void OnTriggerEnter(Collider collider)
    {
        if (!canDealDamage || collider.gameObject.CompareTag(TagGO)) return;
        
        GameObject target = collider.transform.gameObject;
        if (!damaged.Contains(target)) Damage(target);
    }

    private void Damage(GameObject target)
    {
        Debug.Log("DAMAGING");
        if (target.TryGetComponent(out HealthController targetHealth))
        {
            var playerController = target.GetComponentInParent<PlayerController>();
            
            // player is damaged 
            if (playerController)
            {
                if (!playerController.IsShielding)
                {
                    onPlayerDamaged?.Invoke(target);
                    targetHealth.Damage(weaponDamage);
                }
            }

            else
            {
                onEnemyDamaged?.Invoke(target);
                targetHealth.Damage(weaponDamage);
            }
                
        }
        
        damaged.Add(target);
    }

    public void StartDealingDamage()
    {
        damaged.Clear();
        canDealDamage = true;
    }

    public void StopDealingDamage()
        => canDealDamage = false;
}
