using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;

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
        
        
        [SerializeField] private List<DungeonObject> genericRoom;
        [SerializeField] private List<DungeonObject> kitchenRoom;
        [SerializeField] private List<DungeonObject> chestRoom;
        [SerializeField] private List<DungeonObject> skeletonRoom;

        [SerializeField] private GameObject occupiedGO;
        [SerializeField] private float objectInstantiationProbability;
        
        
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
        
        #region Generation
        
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
        
        #endregion
        
        #region Dungeon Instantiation

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
        
        #region Objects Instantiation

        private bool CorridorNextToTile(Rect room, int x, int y)
        {
            for (int i = x - 1; i <= x + 1; i++)
            {
                for (int j = y - 1; j <= y + 1; j++)
                {
                    if (DungeonBoard[i, j] == TileType.Hallway) return true;
                }
            }

            return false;
        }

        private void UpdateObjectsRadius(Vector3 pos, RoomAvailability availability, int radius)
        {
            int posX = (int) pos.x;
            int posY = (int) pos.z;
            
            int xMin = posX - radius < 0 ? 0 : posX - radius;
            int xMax = posX + radius + 1 > boardWidth ? boardWidth : posX + radius + 1;
            int yMin = posY - radius < 0 ? 0 : posY - radius;
            int yMax = posY + radius + 1 > boardHeight ? boardHeight : posY + radius + 1;

            for (int i = xMin; i < xMax; i++)
            {
                for (int j = yMin; j < yMax; j++)
                {
                    roomAvailability[i, j] = availability;
                    // Instantiate(occupiedGO, new Vector3(i*4,1,j*4), Quaternion.identity, parent);
                }
            }
        }

        private void DrawSkeletonRoom(Rect room)
        {
            var corridorEntrance = GetRoomOrientation(room);
            bool topOrBot = corridorEntrance == CorridorDirection.Bottom || corridorEntrance == CorridorDirection.Top;

            var sidePrefab = skeletonRoom.Where(x => x.position == ObjectPosition.Side).ToList()[0];
            var sideOffset = sidePrefab.offset;
                            
            // WE WANT SIDES LEFT / RIGHT
            if (topOrBot)
            {
                for (int i = (int) (room.yMin + 2); i < (room.yMax - 2); i++)
                {
                    // Instantiate(OccupiedGO, new Vector3((int) room.xMin,0,i) * 4, Quaternion.identity, parent);
                    // Instantiate(OccupiedGO, new Vector3((int) room.xMax - 1,0,i) * 4, Quaternion.identity, parent);

                    if (DungeonBoard[(int) room.xMin, i] != TileType.Hallway)
                    {
                        var position = new Vector3((int) (room.xMin + 1), 0, i);
                        Instantiate(sidePrefab.gameObject, position * 4 + sideOffset, Quaternion.identity, parent);
                        UpdateObjectsRadius(position, RoomAvailability.Occupied, sidePrefab.radius);
                    }

                    if (DungeonBoard[(int) room.xMax - 1, i] != TileType.Hallway)
                    {
                        var position = new Vector3((int) (room.xMax - 2), 0, i);
                        Instantiate(sidePrefab.gameObject, position * 4 + sideOffset, Quaternion.identity, parent);
                        UpdateObjectsRadius(position, RoomAvailability.Occupied, sidePrefab.radius);
                    }
                }
            }

            else
            {
                for (int i = (int) (room.xMin + 2); i < (room.xMax - 2); i++)
                {
                    // Instantiate(OccupiedGO, new Vector3(i, 0, (int) room.yMin) * 4, Quaternion.identity, parent);
                    // Instantiate(OccupiedGO, new Vector3(i, 0, (int) room.yMax - 1) * 4, Quaternion.identity, parent);

                    if (DungeonBoard[i, (int) room.yMin] != TileType.Hallway)
                    {
                        var position = new Vector3(i, 0, (int) (room.yMin + 1));
                        Instantiate(sidePrefab.gameObject, position * 4 + sideOffset, Quaternion.identity, parent);
                        UpdateObjectsRadius(position, RoomAvailability.Occupied, sidePrefab.radius);
                    }

                    if (DungeonBoard[i, (int) room.yMax - 1] != TileType.Hallway)
                    {
                        var position = new Vector3(i, 0, (int) (room.yMax - 2));
                        Instantiate(sidePrefab.gameObject, position * 4 + sideOffset, Quaternion.identity, parent);
                        UpdateObjectsRadius(position, RoomAvailability.Occupied, sidePrefab.radius);
                    }
                }
            }
            
            var middlePrefab = skeletonRoom.Where(x => x.position == ObjectPosition.Middle).ToList()[0];
            var middleOffset = middlePrefab.offset;
            
            Vector3 middle = new Vector3 ((int) room.center.x, 0, (int) room.center.y);

            var prefab = corridorEntrance switch
            {
                CorridorDirection.Left => middlePrefab.leftGameObject,
                CorridorDirection.Right => middlePrefab.rightGameObject,
                CorridorDirection.Top => middlePrefab.gameObject,
                _ => middlePrefab.bottomGameObject
            };

            Instantiate(prefab, middle * 4 + middleOffset , Quaternion.identity, parent);
            UpdateObjectsRadius(middle, RoomAvailability.Occupied, middlePrefab.radius);
        }
        
        private void DrawChestRoom(Rect room)
        {
            var sidePrefab = chestRoom.Where(x => x.position == ObjectPosition.Side).ToList()[0];
            var sideOffset = sidePrefab.offset;
            
            
            for (int i = (int) (room.yMin + 1); i < (room.yMax - 1); i++)
            {
                if (!CorridorNextToTile(room, (int) room.xMin + 1, i))
                {
                    var position = new Vector3((int) (room.xMin + 1), 0, i);
                    Instantiate(sidePrefab.gameObject, position * 4 + sideOffset, Quaternion.identity, parent);
                    UpdateObjectsRadius(position, RoomAvailability.Occupied, sidePrefab.radius);
                }

                if (!CorridorNextToTile(room, (int) room.xMax - 2, i))
                {
                    var position = new Vector3((int) (room.xMax - 2), 0, i);
                    Instantiate(sidePrefab.gameObject, position * 4 + sideOffset, Quaternion.identity, parent);
                    UpdateObjectsRadius(position, RoomAvailability.Occupied, sidePrefab.radius);
                }
            }

            for (int i = (int) (room.xMin + 1); i < (room.xMax - 1); i++)
            {
                if (!CorridorNextToTile(room, i, (int) room.yMin + 1))
                {
                    var position = new Vector3(i, 0, (int) (room.yMin + 1));
                    Instantiate(sidePrefab.gameObject, position * 4 + sideOffset, Quaternion.identity, parent);
                    UpdateObjectsRadius(position, RoomAvailability.Occupied, sidePrefab.radius);
                }

                if (!CorridorNextToTile(room, i, (int) room.yMax - 2))
                {
                    var position = new Vector3(i, 0, (int) (room.yMax - 2));
                    Instantiate(sidePrefab.gameObject, position * 4 + sideOffset, Quaternion.identity, parent);
                    UpdateObjectsRadius(position, RoomAvailability.Occupied, sidePrefab.radius);
                }
            }

            var middlePrefab = chestRoom.Where(x => x.position == ObjectPosition.Middle).ToList()[0];
            var middleOffset = middlePrefab.offset;
            
            Vector3 middle = new Vector3 ((int) room.center.x, 0, (int) room.center.y);
            
            Instantiate(middlePrefab.gameObject, middle * 4 + middleOffset , Quaternion.identity, parent);
            UpdateObjectsRadius(middle, RoomAvailability.Occupied, middlePrefab.radius);
        }

        private void DrawKitchen(Rect room)
        {
            var corridorEntrance = GetRoomOrientation(room);
            var sidePrefabList = kitchenRoom.Where(x => x.position == ObjectPosition.Side).ToList();

            switch (corridorEntrance)
            {
                case CorridorDirection.Left:
                    
                    for (int i = (int) (room.yMin + 1); i < (room.yMax - 1); i++)
                    {
                        // Instantiate(OccupiedGO, new Vector3((int) room.xMax - 1,0,i) * 4, Quaternion.identity, parent);
                        
                        if (DungeonBoard[(int) room.xMax - 1, i] != TileType.Hallway)
                        {
                            Random random = new Random();
                            var sidePrefab = sidePrefabList[random.Next(0, sidePrefabList.Count)];
                            var sideOffset = sidePrefab.offset;

                            var position = new Vector3((int) (room.xMax - 2), 0, i);
                            Instantiate(sidePrefab.rightGameObject, position * 4 + sideOffset, Quaternion.identity, parent);
                            UpdateObjectsRadius(position, RoomAvailability.Occupied, sidePrefab.radius);
                        }
                    }

                    break;
                case CorridorDirection.Right:
                    
                    for (int i = (int) (room.yMin + 1); i < (room.yMax - 1); i++)
                    {
                        // Instantiate(OccupiedGO, new Vector3((int) room.xMin,0,i) * 4, Quaternion.identity, parent);

                        if (DungeonBoard[(int) room.xMin, i] != TileType.Hallway)
                        {
                            Random random = new Random();
                            var sidePrefab = sidePrefabList[random.Next(0, sidePrefabList.Count)];
                            var sideOffset = sidePrefab.offset;

                            var position = new Vector3((int) (room.xMin + 1), 0, i);
                            Instantiate(sidePrefab.rightGameObject, position * 4 + sideOffset, Quaternion.identity, parent);
                            UpdateObjectsRadius(position, RoomAvailability.Occupied, sidePrefab.radius);
                        }
                    }
                
                    break;
                case CorridorDirection.Top:
                    
                    for (int i = (int) (room.xMin + 1); i < (room.xMax - 1); i++)
                    {
                        // Instantiate(OccupiedGO, new Vector3(i, 0, (int) room.yMin) * 4, Quaternion.identity, parent);

                        if (DungeonBoard[i, (int) room.yMin] != TileType.Hallway)
                        {
                            Random random = new Random();
                            var sidePrefab = sidePrefabList[random.Next(0, sidePrefabList.Count)];
                            var sideOffset = sidePrefab.offset;
                            
                            var position = new Vector3(i, 0, (int) (room.yMin + 1));
                            Instantiate(sidePrefab.gameObject, position * 4 + sideOffset, Quaternion.identity, parent);
                            UpdateObjectsRadius(position, RoomAvailability.Occupied, sidePrefab.radius);
                        }
                    }
                    
                    break;
                
                case CorridorDirection.Bottom:
                    
                    for (int i = (int) (room.xMin + 1); i < (room.xMax - 1); i++)
                    {
                        // Instantiate(OccupiedGO, new Vector3(i, 0, (int) room.yMax - 1) * 4, Quaternion.identity, parent);
                        
                        if (DungeonBoard[i, (int) room.yMax - 1] != TileType.Hallway)
                        {
                            Random random = new Random();
                            var sidePrefab = sidePrefabList[random.Next(0, sidePrefabList.Count)];
                            var sideOffset = sidePrefab.offset;
                            
                            var position = new Vector3(i, 0, (int) (room.yMax - 2));
                            Instantiate(sidePrefab.gameObject, position * 4 + sideOffset, Quaternion.identity, parent);
                            UpdateObjectsRadius(position, RoomAvailability.Occupied, sidePrefab.radius);
                        }
                    }
                    break;
            }

            var middlePrefab = kitchenRoom.Where(x => x.position == ObjectPosition.Middle).ToList()[0];
            var middleOffset = middlePrefab.offset;
            
            Vector3 middle = new Vector3 ((int) room.center.x, 0, (int) room.center.y);
            
            Instantiate(middlePrefab.gameObject, middle * 4 + middleOffset , Quaternion.identity, parent);
            UpdateObjectsRadius(middle, RoomAvailability.Occupied, middlePrefab.radius);
        }

        private void DrawGenericRoom(Rect room)
        {
            for (int i = (int) room.xMin + 2; i < room.xMax - 2; i++)
            {
                for (int j = (int) room.yMin + 2; j < room.yMax - 2; j++)
                {
                    if (roomAvailability[i, j] == RoomAvailability.Occupied) continue;

                    Random random = new Random();
                    if (random.Next(100) > objectInstantiationProbability) continue;
                    
                    random = new Random();
                    var objectPrefab = genericRoom[random.Next(0, genericRoom.Count)];
                    
                    var position = new Vector3(i, 0, j);

                    Instantiate(objectPrefab.gameObject, position * 4, Quaternion.identity, parent);
                    // Instantiate(occupiedGO, position * 4, Quaternion.identity, parent);
                    UpdateObjectsRadius(position, RoomAvailability.Occupied, objectPrefab.radius);

                }
            }
        }

        #endregion

        private CorridorDirection GetRoomOrientation(Rect room)
        {
            for (int i = (int) room.xMin; i < room.xMax; i++)
            {
                if (DungeonBoard[i, (int) room.yMin] == TileType.Hallway) return CorridorDirection.Bottom;
                if (DungeonBoard[i, (int) room.yMax - 1] == TileType.Hallway) return CorridorDirection.Top;
            }
            
            for (int i = (int) room.yMin; i < room.yMax; i++)
            {
                if (DungeonBoard[(int) room.xMin, i] == TileType.Hallway) return CorridorDirection.Left;
                if (DungeonBoard[(int) room.xMax - 1, i] == TileType.Hallway) return CorridorDirection.Right;
            }
            
            Debug.Log("ORIENTATION ISSUE");
            return CorridorDirection.Bottom;
        }

        public void DrawObjects()
        {
            List<Rect> allRooms = rooms.OrderBy(x=> x.height * x.width).ToList();

            // SPAWN PRIORITY
            // 0 - Spawn Room
            // 1 - Skeleton
            // 2 - Chest
            // 3 - Chest
            // 4 - Kitchen

            int currentRoom = -1;
            while (allRooms.Count > 0)
            {
                currentRoom++;
                
                switch (currentRoom)
                {
                    case 0:
                        DrawGenericRoom(allRooms[0]);
                        allRooms.RemoveAt(0);
                        break;
                    
                    case 1:
                        Rect skelRect = allRooms.Select(x => x).
                            Where(x => x.height > 5 && x.width > 5).
                            OrderByDescending(x=>x.width * x.height).First();

                        DrawSkeletonRoom(skelRect);
                        DrawGenericRoom(skelRect);
                        allRooms.Remove(skelRect);
                        break;
                    
                    case 2 or 3:
                        Rect chestRect = allRooms.Select(x => x).
                            Where(x => x.height > 5 && x.width > 5).
                            OrderByDescending(x=>x.width * x.height).FirstOrDefault();

                        if (chestRect == default) break;
                        
                        DrawChestRoom(chestRect);
                        DrawGenericRoom(chestRect);
                        allRooms.Remove(chestRect); 
                        break;

                    case 4:
                        Rect kitchenRect = allRooms.Select(x => x).
                            Where(x => x.height >= 4 && x.width >= 4).
                            OrderByDescending(x=>x.width * x.height).First();
                        
                        DrawKitchen(kitchenRect);
                        DrawGenericRoom(kitchenRect);

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
