using System;
using Photon.Pun;
using UnityEngine;

namespace Boss
{
    public class BossLevelHandler : MonoBehaviour
    {
        private PhotonView photonView;
        private void Start()
        {
            photonView = GetComponent<PhotonView>();
            if (PhotonNetwork.IsMasterClient)
                photonView.RPC(nameof(TeleportPlayers), RpcTarget.AllBuffered);
        }

        [PunRPC]
        private void TeleportPlayers()
        {
            var player = PhotonNetwork.LocalPlayer.TagObject as GameObject;
            if (player == null) return;

            player.transform.position = new Vector3(-2.5f, 4, 3.5f);
        }
    }
}
