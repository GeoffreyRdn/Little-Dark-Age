using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Enemies
{
    public class EnemyInstantiation : MonoBehaviour
    {
        [SerializeField] private string enemyPath;
        
        public static int EnemiesRemaining = 0;

        public void SpawnEnemies(List<Rect> rooms)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            
            for (var index = 0; index < rooms.Count - 1; index++)
            {
                var room = rooms[index];
                var (x, z) = (room.center.x * 4, room.center.y * 4);
                for (int i = 0; i < Random.Range(2, 5); i++)
                {
                    Vector3 spawnPos = new Vector3(x+Random.Range(-1f, 1f), 0, z + Random.Range(-1f, 1f));
                    var enemy = PhotonNetwork.Instantiate(enemyPath, spawnPos, Quaternion.identity);
                    enemy.transform.parent = transform;
                    
                    EnemiesRemaining++;
                }
            }
            
            Debug.Log("ENEMIES : " + EnemiesRemaining);
        }
    }
}
