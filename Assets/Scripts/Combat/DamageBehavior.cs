using System.Collections.Generic;
using System.Linq;
using Enemies;
using Health;
using NaughtyAttributes;
using Photon.Pun;
using Photon.Realtime;
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
                pv.RPC(nameof(TransmitPlayerDamaged), RpcTarget.All, targetID);
                Damage(collider.gameObject);
            }
        }
        
    }

    [PunRPC]
    private void TransmitPlayerDamaged(int targetID)
        => damaged.Add(targetID);

    private void Damage(GameObject target)
    {
        if (target.TryGetComponent(out HealthController targetHealth))
        {
            var playerController = target.GetComponentInParent<PlayerController>();
            var viewId = target.GetComponent<PhotonView>().ViewID;
            
            // player is damaged 
            if (playerController)
            {
                // NOT SHIELDING
                if (!(playerController.currentState == PlayerController.ShieldEnterAnimation ||
                    playerController.currentState == PlayerController.ShieldStayAnimation))
                {
                    onPlayerDamaged?.Invoke(target);
                    pv.RPC(nameof(DamagePlayer), RpcTarget.All, viewId, weaponDamage);
                }
            }

            else
            {
                onEnemyDamaged?.Invoke(target);
                pv.RPC(nameof(DamageEnemy), RpcTarget.MasterClient, viewId, weaponDamage);
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

    
    [PunRPC]
    private void DamagePlayer(int id, float dmg)
    {
        Player player = PhotonNetwork.PlayerList.FirstOrDefault(x => 
            ((GameObject) x.TagObject).GetComponent<PhotonView>().ViewID == id);
        
        if (player != null)
        {
            var playerGO = player.TagObject as GameObject;
            playerGO.GetComponent<HealthController>().Damage(dmg);
            Debug.Log("PLAYER HEALTH : " + playerGO.GetComponent<HealthController>().Health);
        }
    }

    [PunRPC]
    private void DamageEnemy(int id, float dmg)
    {
        GameObject enemy = EnemyInstantiation.Enemies.FirstOrDefault(x => x.GetComponent<PhotonView>().ViewID == id);
        if (enemy)
        {
            Debug.Log("DAMAGING ENEMY NAMED " + enemy.name);
            enemy.GetComponent<HealthController>().Damage(dmg);
        }
    }
}
