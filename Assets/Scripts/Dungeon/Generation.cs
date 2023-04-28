using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace Dungeon
{
    public class Generation : MonoBehaviour
    {
        #region Variables
        
        [BoxGroup("Dungeon Settings")] [Range(50,200)] [SerializeField] private int boardWidth;
        [BoxGroup("Dungeon Settings")] [Range(50,200)] [SerializeField] private int boardHeight;
        
        [BoxGroup("Dungeon Settings")] [Range(10,200)] [SerializeField] internal int minRoomSize;
        [BoxGroup("Dungeon Settings")] [Range(10,200)] [SerializeField] internal int maxRoomSize;
        
        [BoxGroup("Dungeon Settings")] [Range(1,5)] [SerializeField] internal int minHallwaySize;
        [BoxGroup("Dungeon Settings")] [Range(1,5)] [SerializeField] internal int maxHallwaySize;

        [BoxGroup("Prefabs")] [SerializeField] private GameObject wall;
        [BoxGroup("Prefabs")] [SerializeField] private GameObject floorRoom;
        [BoxGroup("Prefabs")] [SerializeField] private GameObject floorHallway;
        
        [BoxGroup("Parents")] [SerializeField] private Transform parent;
        
        [Tag] [SerializeField] private string deletionTag;

        internal TileType[,] DungeonBoard;

        // getting all the rooms for the AI
        public List<Rect> rooms;

        #endregion

        #region Setter

        public void AddRoom(Rect room)
            => rooms.Add(room);

        #endregion

        #region Methods
        
        [Button]
        public void GenerateDungeon()
        {
            DungeonBoard = new TileType[boardWidth, boardHeight];
            rooms = new List<Rect>();
            
            #if UNITY_EDITOR
                ClearDungeon();
            #endif

            // init the dungeon
            SubDungeon dungeon = new SubDungeon(new Rect(0, 0, boardWidth, boardHeight),
                minHallwaySize, maxHallwaySize);
            // cutting the dungeon into smaller dungeons
            BSP.BinarySpacePartitioning(dungeon, this);
            // creating the rooms and the hallways
            dungeon.GetRoom();

            // retrieving the data from the dungeon and putting it into the array
            GetComponent<GetDungeon>().GetData(dungeon, this);

            // instantiating the dungeon
            DrawBoard();
        }
        
        [Button]
        private void ClearDungeon()
        {
            foreach (GameObject go in GameObject.FindGameObjectsWithTag(deletionTag))
            {
                DestroyImmediate(go);
            }
        }

        private void InstantiateWall(int i, int j)
        {

            // ROOM UP
            if (DungeonBoard[i, j + 1] == TileType.Hallway || DungeonBoard[i, j + 1] == TileType.Room)
            {
                Instantiate(wall, new Vector3(i*4,0, (j+1)*4), Quaternion.identity, parent);
            }

            // ROOM DOWN
            if (DungeonBoard[i, j - 1] == TileType.Hallway || DungeonBoard[i, j - 1] == TileType.Room)
            {
                Instantiate(wall, new Vector3(i*4,0, (j-1)*4), Quaternion.Euler(0,180,0), parent);
            }
            
            // LEFT ROOM
            if (DungeonBoard[i - 1, j] == TileType.Hallway || DungeonBoard[i - 1, j] == TileType.Room)
            {
                Instantiate(wall, new Vector3((i-1)*4,0, j*4), Quaternion.Euler(0,-90,0), parent);
            }

            // ROOM RIGHT
            if (DungeonBoard[i + 1, j] == TileType.Hallway || DungeonBoard[i + 1, j] == TileType.Room)
            {
                Instantiate(wall, new Vector3((i+1)*4,0, j*4), Quaternion.Euler(0,90,0), parent);
            }
        }
        
        private void InstantiateFloor(int i, int j)
        {
            Instantiate(floorRoom, new Vector3(i*4,0, j*4), Quaternion.identity, parent);
        }
        
        private void InstantiateCorridor(int i, int j)
        {
            Instantiate(floorHallway, new Vector3(i*4,0, j*4), Quaternion.identity, parent);
        }

        public void DrawBoard()
        {
            for (int i = 0; i < boardWidth; i++)
            {
                for (int j = 0; j < boardHeight; j++)
                {
                    switch (DungeonBoard[i, j])
                    {
                        case TileType.Wall:
                            InstantiateWall(i,j);
                            break;
                        case TileType.Room:
                            InstantiateFloor(i, j);
                            break;
                        case TileType.Hallway:
                            InstantiateCorridor(i, j);
                            break;
                    }
                }
            }
        }
        
        #endregion
    }
}
