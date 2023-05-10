using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Enemies;
using Health;
using NaughtyAttributes;
using Photon.Pun;
using Photon.Realtime;
using UI;
using UnityEngine;

public class DamageBehavior : MonoBehaviour
{
    [SerializeField] private float weaponDamage;
    [SerializeField, Tag] private string TagGO;

    private PhotonView pv;
    private bool canDealDamage;
    private RaycastHit hit;
    private List<int> damaged;
    
    public delegate void OnPlayerDamaged(GameObject player);
    public delegate void OnEnemyDamaged(GameObject player);
    public static OnPlayerDamaged onPlayerDamaged;
    public static OnEnemyDamaged onEnemyDamaged;

    private void Start()
    {
        pv = GetComponent<PhotonView>();
        damaged = new List<int>();
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (!canDealDamage || collider.gameObject.CompareTag(TagGO)) return;
        
        if (collider.gameObject.TryGetComponent(out PhotonView photonView))
        {
            int targetID = photonView.ViewID;
            if (!damaged.Contains(targetID))
            {
                damaged.Add(targetID);
                Damage(collider.gameObject);
            }
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
                // NOT SHIELDING
                if (!(playerController.currentState == PlayerController.ShieldEnterAnimation ||
                    playerController.currentState == PlayerController.ShieldStayAnimation))
                {
                    onPlayerDamaged?.Invoke(target);
                    target.GetComponent<HealthController>().Damage(weaponDamage);
                }
            }

            else
            {
                onEnemyDamaged?.Invoke(target);
                target.GetComponent<HealthController>().Damage(weaponDamage);
            }
        }
    }

    public void StartDealingDamage()
    {
        damaged.Clear();
        canDealDamage = true;
    }

    public void StopDealingDamage()
        => canDealDamage = false;
    
}
