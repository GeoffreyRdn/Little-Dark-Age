using Photon.Pun;
using UnityEngine;

namespace Lobby
{
    public class LaunchDungeon : MonoBehaviour
    {
        [SerializeField] private string gameScene;

        private PhotonView pv;

        private void Awake()
        {
            pv = GetComponent<PhotonView>();
        }

        private void OnTriggerEnter(Collider player)
        {
            GameObject master = (GameObject) PhotonNetwork.MasterClient.TagObject;
            int masterViewID = master.GetComponent<PhotonView>().ViewID;
            
            if (PhotonNetwork.IsConnectedAndReady && player.GetComponent<PhotonView>().ViewID == masterViewID)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;
                pv.RPC(nameof(EnableLoadingScren), RpcTarget.All);
                
                Debug.Log("Loading Dungeon");
                PhotonNetwork.LoadLevel(gameScene);
            }
        }

        [PunRPC]
        private void EnableLoadingScren()
        {
            Debug.Log("ENABLING LOADING SCREEN");
            if (PhotonNetwork.LocalPlayer.TagObject is GameObject player)
            {
                var pc = player.GetComponent<PlayerController>(); 
                pc.loadingScreen.SetActive(true);
                pc.audioSource.mute = true;
                Debug.Log("LOADING SCREEN ENABLE");

            }
        }
    }
}
