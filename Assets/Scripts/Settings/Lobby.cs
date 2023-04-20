using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;


using TMPro;
using NaughtyAttributes;

using Photon.Pun;
using Photon.Realtime;

namespace Lobby
{
    public class Lobby : MonoBehaviourPunCallbacks
    {
        #region Singleton
        public static Lobby lobby;
        #endregion
    
        #region Variables

        [SerializeField] string gameScene;
        [BoxGroup("Room Creation")] [SerializeField] TMP_InputField inputField;

        [BoxGroup("Room Creation")] [SerializeField] RoomItem roomItemPrefab;
        [BoxGroup("Room Creation")] [SerializeField] Transform contentObject;
        [BoxGroup("Room Creation")] [SerializeField] GameObject Error;
        [BoxGroup("Room Creation")] [SerializeField] TextMeshProUGUI CreateRoomError;
        [BoxGroup("Room Creation")] [SerializeField] TMP_InputField playerName;
        
        [BoxGroup("Loading Data")] [SerializeField] PlayerNameData playerNameData;
        [BoxGroup("Loading Data")] [SerializeField] string savePath;
        readonly List<RoomItem> roomItemList = new List<RoomItem>();
        readonly Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
        
        [SerializeField] float timeBetweenUpdates = 1.5f;
        float nextUpdateTime;

        Coroutine CoroutineImage;
        Coroutine CoroutineOutline;
        Coroutine CoroutineText;
        Coroutine CoroutineCreateRoomError;

        #endregion

        #region Events

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            if (Time.time >= nextUpdateTime)
            {
                UpdateRoomList(roomList);
                nextUpdateTime = Time.time + timeBetweenUpdates;
            }
        }

        public override void OnConnectedToMaster()
        {
            PhotonNetwork.JoinLobby();
            PhotonNetwork.AutomaticallySyncScene = true;
        }

        #endregion

        #region Methods

        void Awake()
            => lobby = lobby == null ? this : lobby;

        void Start()
        {
            playerName.text = playerNameData.playerNameData;
            PhotonNetwork.NickName = playerName.text;
            PhotonNetwork.ConnectUsingSettings();
        }
        
        public void OnChangeNickname()
        {
            PhotonNetwork.NickName = playerName.text == "" || playerName.text.Length < 1 ? "Player" : playerName.text;
            playerNameData.playerNameData = PhotonNetwork.NickName;
        }
        
        public void CreateRoom()
        {
            
            if(inputField.text.Length < 1 || inputField.text == "")
            {
                
                if (CoroutineImage != null)
                {
                    StopCoroutine(CoroutineImage);
                    StopCoroutine(CoroutineOutline);
                    StopCoroutine(CoroutineText);
                }
                return;
            }
            Debug.Log("room created");
            PhotonNetwork.CreateRoom(inputField.text, new RoomOptions() {MaxPlayers = 4});
        }

        public override void OnCreatedRoom()
        {
            base.OnCreatedRoom();
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.LoadLevel(gameScene);
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            base.OnCreateRoomFailed(returnCode, message);
            Debug.Log("failed to create room");
            var image = Error.GetComponent<Image>();
            var outline = Error.GetComponentInChildren<Image>();

            if (CoroutineImage != null)
                StopCoroutine(CoroutineImage);
            if (CoroutineOutline != null)
                StopCoroutine(CoroutineOutline);
            if (CoroutineText != null)
                StopCoroutine(CoroutineText);
                
        }

        void UpdateRoomList(List<RoomInfo> roomList)
        {
            foreach (var item in roomItemList)
                Destroy(item.gameObject);
            roomItemList.Clear();

            foreach (var info in roomList)
            {
                if (info.RemovedFromList)
                    cachedRoomList.Remove(info.Name);
                else
                    cachedRoomList[info.Name] = info;
            }
            
            foreach (var (_, item) in cachedRoomList.Where(item => !item.Value.RemovedFromList))
            {
                var room = Instantiate(roomItemPrefab, contentObject);
                room.SetName(item.Name);
                roomItemList.Add(room);
            }

            foreach (var (name, _) in cachedRoomList.Where(item => item.Value.RemovedFromList))
                cachedRoomList.Remove(name);
        }

        public static void JoinRoom(string roomName)
            => PhotonNetwork.JoinRoom(roomName);

        #endregion
    }
    
    [Serializable] public class PlayerNameData
    {
        public string playerNameData;
        public PlayerNameData()
        {
            playerNameData = "";
        }
    }
}