using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;

namespace Dungeon
{
    public class Generation : MonoBehaviour
    {
        internal enum RoomAvailability
        {
            Available,
            Occupied
        }
        
        internal enum CorridorDirection
        {
            Left,
            Right,
            Top,
            Bottom
        }
        
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
        
        
        [SerializeField] private List<DungeonObject> kitchenRoom;
        [SerializeField] private List<DungeonObject> bedroomRoom;
        [SerializeField] private List<DungeonObject> chestRoom;
        [SerializeField] private List<DungeonObject> skeletonRoom;
        
        
        [BoxGroup("Parents")] [SerializeField] private Transform parent;
        
        [Tag] [SerializeField] private string deletionTag;

        internal TileType[,] DungeonBoard;
        internal RoomAvailability[,] roomAvailability;

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
            roomAvailability = new RoomAvailability[boardWidth, boardHeight];
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

        private void DrawSkeletonRoom(Rect room)
        {
            var corridorEntrance = GetRoomOrientation(room);
            bool topOrBot = corridorEntrance == CorridorDirection.Bottom || corridorEntrance == CorridorDirection.Top;

            var sidePrefab = skeletonRoom.Where(x => x.position == ObjectPosition.NotCorridorSideAndOpposite).ToList()[0];
            var sideOffset = sidePrefab.offset;
            
            Debug.Log("TOP/BOT : " + topOrBot);
            
            // WE WANT SIDES LEFT / RIGHT
            if (topOrBot)
            {
                for (int i = (int) (room.yMin + 1); i < (room.yMax - 1); i++)
                {
                    if (DungeonBoard[(int) room.xMin, i] != TileType.Hallway)
                    {
                        Instantiate(sidePrefab.gameObject,
                            new Vector3((int) (room.xMin +1) * 4, 0, i * 4) + sideOffset,
                            Quaternion.identity, parent);
                    }

                    if (DungeonBoard[(int) room.xMax, i] != TileType.Hallway)
                    {
                        Instantiate(sidePrefab.gameObject, 
                            new Vector3((int) (room.xMax - 2) * 4, 0, i * 4) + sideOffset, 
                            Quaternion.identity, parent);
                    }
                }
            }

            else
            {
                for (int i = (int) (room.xMin + 1); i < (room.xMax - 1); i++)
                {
                    if (DungeonBoard[(int) room.xMin, i] != TileType.Hallway)
                    {
                        Instantiate(sidePrefab.gameObject, 
                            new Vector3(i*4, 0, (int) (room.yMin +1) * 4) + sideOffset,
                            Quaternion.identity, parent);
                    }

                    if (DungeonBoard[(int) room.xMax, i] != TileType.Hallway)
                    {
                        Instantiate(sidePrefab.gameObject, 
                            new Vector3(i*4, 0, (int) (room.yMax - 2) * 4) + sideOffset, 
                            Quaternion.identity, parent);
                    }
                }
            }
            
            var middlePrefab = skeletonRoom.Where(x => x.position == ObjectPosition.Middle).ToList()[0];
            var middleOffset = middlePrefab.offset;
            Vector3 middle = new Vector3 ((int) room.center.x * 4, 0, (int) room.center.y * 4);
            Instantiate(middlePrefab.gameObject, middle + middleOffset , Quaternion.identity, parent);
        }
        
        private void DrawChestRoom(Rect room)
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
        

        private void DrawKitchen(Rect room)
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

        private CorridorDirection GetRoomOrientation(Rect room)
        {
            for (int i = (int) room.xMin; i < room.xMax; i++)
            {
                if (DungeonBoard[i, (int) room.yMin] == TileType.Hallway) return CorridorDirection.Top;
                if (DungeonBoard[i, (int) room.yMax] == TileType.Hallway) return CorridorDirection.Bottom;
            }
            
            for (int i = (int) room.yMin; i < room.yMax; i++)
            {
                if (DungeonBoard[(int) room.xMin, i] == TileType.Hallway) return CorridorDirection.Left;
                if (DungeonBoard[(int) room.xMax, i] == TileType.Hallway) return CorridorDirection.Right;
            }

            return CorridorDirection.Bottom;
        }
        

        public void DrawObjects()
        {
            List<Rect> allRooms = rooms.OrderBy(x=> x.height * x.width).ToList();

            // SPAWN PRIORITY
            // 0 - Spawn Room
            // 1 - Skeleton
            // 2 - Chest
            // 3 - Bedroom
            // 4 - Kitchen

            int currentRoom = -1;
            while (allRooms.Count > 0)
            {
                currentRoom++;
                
                switch (currentRoom)
                {
                    case 0:
                        DrawSpawnRoom(allRooms[0]);
                        allRooms.RemoveAt(0);
                        break;
                    
                    case 1:
                        Rect skelRect = allRooms.Select(x => x).
                            Where(x => x.height >= 5 && x.width >= 5).
                            OrderByDescending(x=>x.width * x.height).First();

                        DrawSkeletonRoom(skelRect);
                        allRooms.Remove(skelRect);
                        break;
                    
                    case 2:
                        Rect chestRect = allRooms.Select(x => x).
                            Where(x => x.height >= 5 && x.width >= 5).
                            OrderByDescending(x=>x.width * x.height).First();
                        
                        DrawChestRoom(chestRect);
                        allRooms.Remove(chestRect); 
                        break;
                    
                    case 3:
                        DrawBedroom(allRooms[0]);
                        allRooms.RemoveAt(0);
                        break;
                    
                    case 4:
                        Rect kitchenRect = allRooms.Select(x => x).
                            Where(x => x.height >= 4 && x.width >= 4).
                            OrderByDescending(x=>x.width * x.height).First();
                        
                        DrawKitchen(kitchenRect);
                        allRooms.Remove(kitchenRect); 
                        break;

                    default:
                        DrawGenericRoom(allRooms[0]);
                        allRooms.RemoveAt(0);
                        break;
                }
            }
        }
        
        #endregion
    }
}
