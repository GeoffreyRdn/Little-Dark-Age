using NaughtyAttributes;
using Photon.Pun;
using UnityEngine;

namespace Dungeon
{
    public class HandleGate : MonoBehaviour
    {
        [SerializeField, Tag] private string playerTag;
        
        private Animator animator;
        private PhotonView pv;
        private static readonly int Open = Animator.StringToHash("Open");
        private static readonly int Close = Animator.StringToHash("Close");

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            pv = GetComponent<PhotonView>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            
            animator.SetTrigger(Open);
            pv.RPC(nameof(SendAnimation), RpcTarget.Others, true);

        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            
            animator.SetTrigger(Close);
            pv.RPC(nameof(SendAnimation), RpcTarget.Others, false);
        }

        [PunRPC]
        private void SendAnimation(bool isOpen)
            => animator.SetTrigger(isOpen ? Open : Close);
    }
}
