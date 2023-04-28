using Photon.Pun;
using UnityEngine;

namespace Lobby
{
    public class LaunchDungeon : MonoBehaviour
    {
        [SerializeField] private string gameScene;
        private void OnTriggerEnter(Collider other)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;
                Debug.Log("Loading Dungeon");
                PhotonNetwork.LoadLevel(gameScene);
            }
        }
    }
}
