using Photon.Pun;
using UnityEngine;

using TMPro;

namespace Lobby
{
    public class RoomItem : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI roomName;
    
        public void SetName(string roomName)
            => this.roomName.text = roomName;

        public void OnClick()
            => Lobby.JoinRoom(roomName.text);
    }
}