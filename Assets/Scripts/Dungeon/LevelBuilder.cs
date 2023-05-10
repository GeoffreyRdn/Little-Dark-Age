using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Enemies;
using NaughtyAttributes;
using Photon.Pun;
using Unity.AI.Navigation;
using UnityEngine;

namespace Dungeon
{
    public class LevelBuilder : MonoBehaviourPun
    {
        [SerializeField] private GameObject enemiesHolder;
        [SerializeField] [Tag] private string environmentTag;
        
        private Generation generation;
        private PhotonView photonView;
        private Vector3 spawnPoint;
        
        private NavMeshSurface surface;

        private void Awake()
        {
            generation = GetComponent<Generation>();
            photonView = GetComponent<PhotonView>();
        }

        private void Start()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                generation.GenerateDungeon();
                Debug.Log("Generation DONE");  
                
                Rect room = generation.rooms.OrderByDescending(x => x.height * x.width).ToList()[^1];
                spawnPoint = new Vector3(room.center.x * 4, 1, room.center.y * 4);
                photonView.RPC(nameof(TransmitSpawnPoint), RpcTarget.OthersBuffered, spawnPoint.x, spawnPoint.z);
                
                // create the navMesh for enemies / spawn enemies
                surface = GameObject.Find("Dungeon").GetComponent<NavMeshSurface>();
                surface.BuildNavMesh();
                
                List<Rect> roomsOrdered = generation.rooms.OrderByDescending(x=> x.width * x.height).ToList();
                enemiesHolder.GetComponent<EnemyInstantiation>().SpawnEnemies(roomsOrdered);
                Debug.Log("Enemies SPAWNED");
                
                StartCoroutine(TransmitGeneration(.5f));
            }
        }

        #region RPC
        

        [PunRPC]
        private void SetMapData(int[][] mapData, int[][] objectData, float[] x, float[]y, float[] width, float[] height)
        {
            int nbRooms = x.Length;
            List<Rect> rooms = new List<Rect>();
            
            for (int i = 0; i < nbRooms; i++)
            {
                rooms.Add(new Rect(x[i], y[i], width[i], height[i]));
            }
            
            Debug.Log("Setting map data");
            generation.DungeonBoard = mapData.ArrayArrayToArray2d();
            generation.ObjectsBoard = objectData.ArrayArrayToArray2dINT();
            generation.rooms = rooms;
            
            Debug.Log("ROOMS : " + generation.rooms);
            
            generation.DrawBoard();
            generation.DrawObjects();
        }

        
        [PunRPC]
        private void TpPlayersToDungeon()
        {
            Debug.Log("Moving Players");
            var currentPlayer = PhotonNetwork.LocalPlayer.TagObject as GameObject;
            
            if (currentPlayer == null) return;
            
            var controller = currentPlayer.GetComponent<CharacterController>();
            controller.enabled = false;
            controller.gameObject.transform.position = spawnPoint;
            controller.enabled = true;
        }

        [PunRPC]
        private void TransmitSpawnPoint(float x, float z)
        {
            spawnPoint = new Vector3(x, 5, z);
        }
        
        [PunRPC]
        private void DisableLoadingScren()
        {
            if (PhotonNetwork.LocalPlayer.TagObject is GameObject player)
            {
                var playerController = player.GetComponent<PlayerController>();
                playerController.loadingScreen.SetActive(false);
            }
        }

        #endregion
        
        
        #region Methods
        
        private IEnumerator TransmitGeneration(float delay)
        {
            yield return new WaitForSeconds(delay);

            Debug.Log("SetMapData");

            int nbRooms = generation.rooms.Count;
            
            float[] x = new float[nbRooms];
            float[] y = new float[nbRooms];
            float[] width = new float[nbRooms];
            float[] height = new float[nbRooms];

            for (int i = 0; i < nbRooms; i++)
            {
                x[i] = generation.rooms[i].x;
                y[i] = generation.rooms[i].y;
                width[i] = generation.rooms[i].width;
                height[i] = generation.rooms[i].height;
            }
                

            photonView.RPC(nameof(SetMapData), RpcTarget.OthersBuffered, 
                generation.DungeonBoard.Array2dToArrayArray(), generation.ObjectsBoard.Array2dToArrayArrayINT(),
                    x,y,width,height);
            StartCoroutine(MovePlayers());
        }

        private IEnumerator MovePlayers()
        {
            yield return new WaitForSeconds(.3f);
            photonView.RPC(nameof(TpPlayersToDungeon), RpcTarget.AllBuffered);
            
            yield return new WaitForSeconds(.2f);
            photonView.RPC(nameof(DisableLoadingScren), RpcTarget.All);
            
        }

        #endregion
    }
}