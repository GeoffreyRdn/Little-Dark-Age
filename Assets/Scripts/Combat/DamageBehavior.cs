using System;
using System.Collections.Generic;
using Health;
using UnityEngine;

public class DamageBehavior : MonoBehaviour
{
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private float weaponLength;
    [SerializeField] private float weaponDamage;
    [SerializeField] private Vector3 raycastOffset;
    
    
    private bool canDealDamage;
    private RaycastHit hit;
    private List<GameObject> damaged;

    private void Start()
        => damaged = new List<GameObject>();

    private void Update()
    {
        if (!canDealDamage) return;

        if (Physics.Raycast(transform.position + raycastOffset, -transform.right, out hit, weaponLength, targetMask))
        {
            GameObject target = hit.transform.gameObject;
            if (!damaged.Contains(target)) Damage(target);
        }
    }

    private void Damage(GameObject target)
    {
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

    private void OnDrawGizmos()
    {
        var position = transform.position + raycastOffset;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(position, position - transform.right * weaponLength);
    }
}
