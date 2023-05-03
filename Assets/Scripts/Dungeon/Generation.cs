using System;
using System.Collections.Generic;
using System.Linq;
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

        #region Throne room
        
        [BoxGroup("Throne Room"), SerializeField] private GameObject throneGO;
        
        #endregion
        
        #region Buffet room
        
        [BoxGroup("Buffet Room"), SerializeField] private GameObject buffetGO;
        
        #endregion
        
        #region Wine Cellar Room
        
        [BoxGroup("Wine Cellar Room"), SerializeField] private GameObject wineGO;
        
        #endregion
        
        #region Office Room
        
        [BoxGroup("Office Room"), SerializeField] private GameObject officeGO;
        
        #endregion
        
        #region Bedroom
        
        [BoxGroup("Bedroom Room"), SerializeField] private GameObject bedroomGO;
        
        #endregion
        
        #region Spawn Room
        
        [BoxGroup("Spawn Room"), SerializeField] private GameObject spawnGO;
        
        #endregion
        
        #region Generic Room
        
        [BoxGroup("Generic Room"), SerializeField] private GameObject genericGO;
        
        #endregion
        
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
            DrawObjects();
        }
        
        [Button]
        public void ClearDungeon()
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
        
        #region Draw Rooms
        
        private void DrawBossRoom(Rect room)
        {
            // Debug.Log("Boss room : " + room.x + " - " + room.y + " - " + room.width * room.height);
        }

        private void DrawSpawnRoom(Rect room)
        {
            // Debug.Log("Spawn : " + room.x + " - " + room.y + " - " + room.width * room.height);
            
            for (int i = (int) room.xMin + 1; i < room.xMax - 1; i++)
            {
                for (int j = (int) room.yMin + 1; j < room.yMax - 1; j++)
                {
                    // Instantiate(spawnGO, new Vector3(i * 4, 0,j * 4), Quaternion.identity, parent);
                }
            }
        }

        private void DrawThroneRoom(Rect room)
        {
            // Debug.Log("Throne : " + room.x + " - " + room.y + " - " + room.width * room.height);
            
            for (int i = (int) room.xMin + 1; i < room.xMax - 1; i++)
            {
                for (int j = (int) room.yMin + 1; j < room.yMax - 1; j++)
                {
                    // Instantiate(throneGO, new Vector3(i * 4, 0,j * 4), Quaternion.identity, parent);
                }
            }
        }


        private void DrawBuffet(Rect room)
        {
            // Debug.Log("Buffet : " + room.x + " - " + room.y + " - " + room.width * room.height);
            
            for (int i = (int) room.xMin + 1; i < room.xMax - 1; i++)
            {
                for (int j = (int) room.yMin + 1; j < room.yMax - 1; j++)
                {
                    // Instantiate(buffetGO, new Vector3(i * 4, 0,j * 4), Quaternion.identity, parent);
                }
            }
        }


        private void DrawBedroom(Rect room)
        {
            // Debug.Log("Bedroom : " + room.x + " - " + room.y + " - " + room.width * room.height);
            
            for (int i = (int) room.xMin + 1; i < room.xMax - 1; i++)
            {
                for (int j = (int) room.yMin + 1; j < room.yMax - 1; j++)
                {
                    // Instantiate(bedroomGO, new Vector3(i * 4, 0,j * 4), Quaternion.identity, parent);
                }
            }
        }


        private void DrawOffice(Rect room)
        {
            // Debug.Log("Office : " + room.x + " - " + room.y + " - " + room.width * room.height);
            
            for (int i = (int) room.xMin + 1; i < room.xMax - 1; i++)
            {
                for (int j = (int) room.yMin + 1; j < room.yMax - 1; j++)
                {
                    // Instantiate(officeGO, new Vector3(i * 4, 0,j * 4), Quaternion.identity, parent);
                }
            }
        }

        private void DrawWineCellar(Rect room)
        {
            // Debug.Log("Wine cellar : " + room.x + " - " + room.y + " - " + room.width * room.height);
            
            for (int i = (int) room.xMin + 1; i < room.xMax - 1; i++)
            {
                for (int j = (int) room.yMin + 1; j < room.yMax - 1; j++)
                {
                    // Instantiate(wineGO, new Vector3(i * 4, 0,j * 4), Quaternion.identity, parent);
                }
            }
        }

        private void DrawGenericRoom(Rect room)
        {
            // Debug.Log("Generic room : " + room.x + " - " + room.y + " - " + room.width * room.height);
            
            for (int i = (int) room.xMin + 1; i < room.xMax - 1; i++)
            {
                for (int j = (int) room.yMin + 1; j < room.yMax - 1; j++)
                {
                    // Instantiate(genericGO, new Vector3(i * 4, 0,j * 4), Quaternion.identity, parent);
                }
            }
        }
        
        #endregion

        public void DrawObjects()
        {
            List<Rect> roomsOrdered = rooms.OrderByDescending(x=> x.width * x.height).ToList();

            if (roomsOrdered.Count == 1)
            {
                DrawBossRoom(roomsOrdered[0]);
                roomsOrdered.RemoveAt(0);
                return;
            }
            
            // spawn
            Rect spawnRoom = roomsOrdered[^1];
            DrawSpawnRoom(spawnRoom);
            roomsOrdered.Remove(spawnRoom);
            
            if (roomsOrdered.Count > 5)
            {
                // biggest (throne)
                Rect throneRoom = roomsOrdered[0];
                DrawThroneRoom(throneRoom);
                roomsOrdered.Remove(throneRoom);

                // 2nd biggest (buffet)
                Rect buffetRoom = roomsOrdered[0];
                DrawBuffet(buffetRoom);
                roomsOrdered.Remove(buffetRoom);

                // 3rd biggest (wine cellar)
                Rect kitchen = roomsOrdered[0];
                DrawWineCellar(kitchen);
                roomsOrdered.Remove(kitchen);

                // smallest room (office)
                Rect officeRoom = roomsOrdered[^1];
                DrawOffice(officeRoom);
                roomsOrdered.Remove(officeRoom);

                // 2nd smallest room (bedroom)
                Rect bedroom = roomsOrdered[^1];
                DrawBedroom(bedroom);
                roomsOrdered.Remove(bedroom);
            }

            foreach (var room in roomsOrdered)
            {
                DrawGenericRoom(room);
            }
        }
        
        #endregion
    }
}
