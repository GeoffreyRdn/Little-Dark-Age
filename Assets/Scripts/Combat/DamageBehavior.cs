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
                if (!playerController.IsShielding) targetHealth.Damage(weaponDamage);
            } 
            
            else targetHealth.Damage(weaponDamage);
                
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
