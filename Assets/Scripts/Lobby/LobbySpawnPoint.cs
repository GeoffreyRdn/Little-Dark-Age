using System;
using Health;
using Photon.Pun;
using UnityEngine;

public class LobbySpawnPoint : MonoBehaviour
{
    [SerializeField] private Vector3 spawnPoint;

    private PhotonView pv;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    private void Start()
    {
        var player = PhotonNetwork.LocalPlayer.TagObject as GameObject;
        if (player == null) return;
        
        var controller = player.GetComponent<CharacterController>();
        controller.enabled = false;
        controller.gameObject.transform.position = spawnPoint;
        controller.enabled = true;
        
        if (PhotonNetwork.IsMasterClient) pv.RPC(nameof(ResetPlayers), RpcTarget.All);
        
        Debug.Log("Player " + player.name + " was tp to " + spawnPoint + " - current pos : " + player.transform.position) ;
    }
    
    [PunRPC]
    private void ResetPlayers()
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            Debug.Log("RESETTING " + player.NickName);
            
            GameObject playerGO = player.TagObject as GameObject;
            var health = playerGO.GetComponent<HealthController>();
            health.Heal(health.MaxHealth);

            playerGO.GetComponent<PlayerController>().isDead = false;
        }
    }
}
