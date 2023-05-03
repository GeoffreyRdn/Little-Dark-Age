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
            PlayerController.isLoadingScene = true;

            if (PhotonNetwork.IsMasterClient)
            {
                generation.GenerateDungeon();
                Debug.Log("Generation DONE");  
                
                Rect room = generation.rooms.OrderByDescending(x => x.height * x.width).ToList()[^1];
                spawnPoint = new Vector3(room.center.x * 4, 1, room.center.y * 4);
                
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
        private void SetMapData(int[][] mapData)
        {
            Debug.Log("Setting map data");
            generation.DungeonBoard = mapData.ArrayArrayToArray2d();
            generation.DrawBoard();
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
            
            Debug.Log("Player " + currentPlayer.name + " moved to : " + spawnPoint + " - actual position : " + currentPlayer.transform.position);

            // Collider[] hitColliders = Physics.OverlapSphere(currentPlayer.transform.GetChild(0).transform.position, 1);
            // foreach (var collider in hitColliders.Where(col => col.CompareTag(environmentTag))) Destroy(collider.gameObject);
            //
            
            PlayerController.isLoadingScene = false;
            // // TODO : remove loading screen
        }

        #endregion
        
        
        #region Methods
        
        private IEnumerator TransmitGeneration(float delay)
        {
            yield return new WaitForSeconds(delay);

            Debug.Log("SetMapData");
            photonView.RPC(nameof(SetMapData), RpcTarget.OthersBuffered, generation.DungeonBoard.Array2dToArrayArray());
            StartCoroutine(MovePlayers());
        }

        private IEnumerator MovePlayers()
        {
            yield return new WaitForSeconds(.2f);
            photonView.RPC(nameof(TpPlayersToDungeon), RpcTarget.AllBuffered);
        }

        #endregion
    }
}