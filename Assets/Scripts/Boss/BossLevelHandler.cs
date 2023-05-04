using System;
using Health;
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

            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            controller.gameObject.transform.position = new Vector3(-2.5f, 4, 3.5f);
            controller.enabled = true;
        }
    }
}
