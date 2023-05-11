using Photon.Pun;
using UnityEngine;
using System.IO;
using System.Linq;
using Cinemachine;
using NaughtyAttributes;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private Vector3 spawnPoint;
    [SerializeField] private string playerLocation;
    [SerializeField, Tag] private string weaponTag;
    
    PhotonView pv;
    
    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    private void Start()
    {
        if (pv.IsMine && PhotonNetwork.LocalPlayer.TagObject == null)
        {
            CreateController();
        }
    }

    private void CreateController()
    {
        var player = PhotonNetwork.Instantiate(playerLocation, spawnPoint, Quaternion.identity);
        
        if (((GameObject) PhotonNetwork.LocalPlayer.TagObject).GetComponent<PhotonView>().ViewID ==
            player.GetComponent<PhotonView>().ViewID)
        {
            Debug.Log("CREATING LOCAL PLAYER");
            var weapons = player.GetComponentsInChildren<DamageBehavior>();
            foreach (var weapon in weapons)
            {
                weapon.gameObject.GetComponent<MeshCollider>().isTrigger = true;
            }
        }
    }
}