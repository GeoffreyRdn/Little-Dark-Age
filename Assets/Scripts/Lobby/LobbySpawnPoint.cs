using Photon.Pun;
using UnityEngine;

public class LobbySpawnPoint : MonoBehaviour
{
    [SerializeField] private Vector3 spawnPoint;
    private void Start()
    {
        var player = PhotonNetwork.LocalPlayer.TagObject as GameObject;
        if (player == null) return;
        
        var controller = player.GetComponent<CharacterController>();
        controller.enabled = false;
        controller.gameObject.transform.position = spawnPoint;
        controller.enabled = true;
        
        Debug.Log("Player " + player.name + " was tp to " + spawnPoint + " - current pos : " + player.transform.position) ;
    }
}
