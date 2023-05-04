using Photon.Pun;
using UnityEngine;
using System.IO;
using System.Linq;
using Cinemachine;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private Vector3 spawnPoint;
    [SerializeField] private string playerLocation;
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
        PhotonNetwork.Instantiate(playerLocation, spawnPoint, Quaternion.identity);
    }
}