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
                
                // create the navMesh for enemies / spawn enemies
                surface = GameObject.Find("Dungeon").GetComponent<NavMeshSurface>();
                surface.BuildNavMesh();
                
                enemiesHolder.GetComponent<EnemyInstantiation>().SpawnEnemies(generation.rooms);
                Debug.Log("Enemies SPAWNED");
                
                // RemoveGravity();
                photonView.RPC(nameof(RemoveGravity), RpcTarget.AllBuffered);
                StartCoroutine(TransmitGeneration(1));
            }
        }

        #region RPC

        [PunRPC]
        private void RemoveGravity()
        {
            var currentPlayer = PhotonNetwork.LocalPlayer.TagObject as GameObject;
            if (currentPlayer == null) return;
            
            PlayerController.isLoadingScene = true;
            PlayerController.applyGravity = false;
        }
        
        [PunRPC]
        private void GetAllRooms(object[] rooms)
        {
            List<Rect> allRooms = rooms.Cast<Rect>().ToList();
            generation.rooms = allRooms;
        }

        [PunRPC]
        private void SetMapData(int[][] mapData)
        {
            generation.DungeonBoard = mapData.ArrayArrayToArray2d();
            generation.DrawBoard();
        }

        [PunRPC]
        private void TpPlayersToDungeon(float x, float z)
        {
            Debug.Log("Moving Players");
            var currentPlayer = PhotonNetwork.LocalPlayer.TagObject as GameObject;

            if (currentPlayer == null)
            {
                Debug.Log("No player");
                return;
            }
            
            Vector3 tpPosition = new Vector3(x, 1.5f, z);
            currentPlayer.transform.position = tpPosition;
            
            PlayerController.applyGravity = true;

            Collider[] hitColliders = Physics.OverlapSphere(currentPlayer.transform.GetChild(0).transform.position, 1);
            foreach (var collider in hitColliders.Where(col => col.CompareTag(environmentTag))) Destroy(collider.gameObject);
            
            PlayerController.isLoadingScene = false;
        }

        #endregion
        
        
        #region Methods
        
        private IEnumerator TransmitGeneration(float delay)
        {
            yield return new WaitForSeconds(delay);

            // object[] roomsObjects = new object[generation.rooms.Count];
            //
            // for (int i = 0; i < generation.rooms.Count; i++)
            // {
            //     roomsObjects[i] = generation.rooms[i];
            // }
            
            // Debug.Log("GetAllRooms");
            // photonView.RPC(nameof(GetAllRooms), RpcTarget.OthersBuffered, roomsObjects);
            // Debug.Log("SetMapData");
            // photonView.RPC(nameof(SetMapData), RpcTarget.OthersBuffered, generation.DungeonBoard.Array2dToArrayArray());
            
            MovePlayers();
        }

        private void MovePlayers()
        {
            var room = generation.rooms[0];
            var center = room.center * 4;
            photonView.RPC(nameof(TpPlayersToDungeon), RpcTarget.AllBuffered, center.x, center.y);
        }

        #endregion
    }
}
