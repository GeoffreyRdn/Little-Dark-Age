using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;

namespace Dungeon
{
    public class Generation : MonoBehaviour
    {
        #region Enums
        
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
        
        #endregion
        
        #region Variables
        
        [BoxGroup("Dungeon Settings")] [Range(50,200)] [SerializeField] private int boardWidth;
        [BoxGroup("Dungeon Settings")] [Range(50,200)] [SerializeField] private int boardHeight;
        
        [BoxGroup("Dungeon Settings")] [Range(10,200)] [SerializeField] internal int minRoomSize;
        [BoxGroup("Dungeon Settings")] [Range(10,200)] [SerializeField] internal int maxRoomSize;
        
        [BoxGroup("Dungeon Settings")] [Range(1,5)] [SerializeField] internal int minHallwaySize;
        [BoxGroup("Dungeon Settings")] [Range(1,5)] [SerializeField] internal int maxHallwaySize;

        [BoxGroup("Prefabs")] [SerializeField] private GameObject basicWallGO;
        [BoxGroup("Prefabs")] [SerializeField] private GameObject wallTorchGO;
        [BoxGroup("Prefabs")] [SerializeField] private GameObject wallWindowGO;
        [BoxGroup("Prefabs")] [SerializeField] private GameObject floorRoom;
        [BoxGroup("Prefabs")] [SerializeField] private GameObject floorHallway;
        [BoxGroup("Prefabs")] [SerializeField] private GameObject doorGO;
        [BoxGroup("Prefabs")] [SerializeField] private float wallWindowProba = 30;
        
        
        [SerializeField] private List<DungeonObject> genericRoom;
        [SerializeField] private List<DungeonObject> kitchenRoom;
        [SerializeField] private List<DungeonObject> chestRoom;
        [SerializeField] private List<DungeonObject> skeletonRoom;

        [BoxGroup("Debug"), SerializeField] private GameObject occupiedGO;
        [BoxGroup("Debug"), SerializeField] private GameObject occupiedGO2;
        [BoxGroup("Debug"), SerializeField] private float objectInstantiationProbability;
        [BoxGroup("Debug"), SerializeField] private bool isHost;
        
        
        [BoxGroup("Parents")] [SerializeField] private Transform parent;
        
        [Tag] [SerializeField] private string deletionTag;

        internal TileType[,] DungeonBoard;
        internal int[,] ObjectsBoard;
        internal RoomAvailability[,] roomAvailability;

        // getting all the rooms for the AI
        [HideInInspector] public List<Rect> rooms;

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
            #if UNITY_EDITOR
            ClearDungeon();
            #endif

            if (!PhotonNetwork.IsConnectedAndReady && !isHost)
            {
                Debug.Log("NOT AN HOST");
                DrawBoard();
                DrawObjects();
                return;
            }
            
            DungeonBoard = new TileType[boardWidth, boardHeight];
            ObjectsBoard = new int [boardWidth, boardHeight];
            rooms = new List<Rect>();

            // init the dungeon
            SubDungeon dungeon = new SubDungeon(new Rect(0, 0, boardWidth, boardHeight),
                minHallwaySize, maxHallwaySize);
            // cutting the dungeon into smaller dungeons
            BSP.BinarySpacePartitioning(dungeon, this);
            // creating the rooms and the hallways
            dungeon.GetRoom();

            // retrieving the data from the dungeon and putting it into the array
            GetComponent<GetDungeon>().GetData(dungeon, this, wallWindowProba);

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

        private void InstantiateWall(int i, int j, GameObject wall)
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
        
        private void InstantiateDoor(int i, int j, GameObject doorPrefab)
        {
            // ROOM UP
            if (DungeonBoard[i, j + 1] == TileType.Room)
            {
                Instantiate(doorPrefab, new Vector3(i*4,0, (j+1)*4), Quaternion.identity, parent);
            }

            // ROOM DOWN
            else if (DungeonBoard[i, j - 1] == TileType.Room)
            {
                Instantiate(doorPrefab, new Vector3(i*4,0, (j-1)*4), Quaternion.Euler(0,180,0), parent);
            }
            
            // LEFT ROOM
            else if (DungeonBoard[i - 1, j] == TileType.Room)
            {
                Instantiate(doorPrefab, new Vector3((i-1)*4,0, j*4), Quaternion.Euler(0,-90,0), parent);
            }

            // ROOM RIGHT
            else if (DungeonBoard[i + 1, j] == TileType.Room)
            {
                Instantiate(doorPrefab, new Vector3((i+1)*4,0, j*4), Quaternion.Euler(0,90,0), parent);
            }
        }
        
        private void InstantiateFloor(int i, int j)
        {
            Instantiate(floorRoom, new Vector3(i*4,0, j*4), Quaternion.identity, parent);
        }
        
        private void InstantiateCorridor(int i, int j)
        {
            Instantiate(floorHallway, new Vector3(i*4,0, j*4), Quaternion.identity, parent);

            for (int k = i-1; k < i+2; k++)
            {
                for (int l = j-1; l < j+2; l++)
                {
                    if (DungeonBoard[k, l] == TileType.Room) InstantiateDoor(i, j, doorGO);
                }
            }
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
                            InstantiateWall(i,j, basicWallGO);
                            break;
                        case TileType.WallTorch:
                            InstantiateWall(i, j, wallTorchGO);
                            break;
                        case TileType.WallWindow:
                            InstantiateWall(i, j, wallWindowGO);
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

        private bool CorridorNextToTile(int x, int y)
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
                if (!CorridorNextToTile((int) room.xMin + 1, i))
                {
                    var position = new Vector3((int) (room.xMin + 1), 0, i);
                    Instantiate(sidePrefab.gameObject, position * 4 + sideOffset, Quaternion.identity, parent);
                    UpdateObjectsRadius(position, RoomAvailability.Occupied, sidePrefab.radius);
                }

                if (!CorridorNextToTile((int) room.xMax - 2, i))
                {
                    var position = new Vector3((int) (room.xMax - 2), 0, i);
                    Instantiate(sidePrefab.gameObject, position * 4 + sideOffset, Quaternion.identity, parent);
                    UpdateObjectsRadius(position, RoomAvailability.Occupied, sidePrefab.radius);
                }
            }

            for (int i = (int) (room.xMin + 1); i < (room.xMax - 1); i++)
            {
                if (!CorridorNextToTile(i, (int) room.yMin + 1))
                {
                    var position = new Vector3(i, 0, (int) (room.yMin + 1));
                    Instantiate(sidePrefab.gameObject, position * 4 + sideOffset, Quaternion.identity, parent);
                    UpdateObjectsRadius(position, RoomAvailability.Occupied, sidePrefab.radius);
                }

                if (!CorridorNextToTile(i, (int) room.yMax - 2))
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

            Debug.Log("DRAWING KITCHEN HOST");
            switch (corridorEntrance)
            {
                case CorridorDirection.Left:
                    
                    for (int i = (int) (room.yMin + 1); i < (room.yMax - 1); i++)
                    {
                        // Instantiate(OccupiedGO, new Vector3((int) room.xMax - 1,0,i) * 4, Quaternion.identity, parent);
                        
                        if (DungeonBoard[(int) room.xMax - 1, i] != TileType.Hallway)
                        {
                            if ((PhotonNetwork.IsConnectedAndReady && PhotonNetwork.IsMasterClient) || isHost)
                            {
                                Random random = new Random();
                                var sidePrefab = sidePrefabList[random.Next(0, sidePrefabList.Count)];
                                var sideOffset = sidePrefab.offset;
                                ObjectsBoard[(int) (room.xMax - 2), i] = sidePrefab.index;


                                var position = new Vector3((int) (room.xMax - 2), 0, i);
                                Instantiate(sidePrefab.rightGameObject, position * 4 + sideOffset, Quaternion.identity, parent);
                                UpdateObjectsRadius(position, RoomAvailability.Occupied, sidePrefab.radius);
                            }

                            else
                            {
                                var prefab = kitchenRoom.
                                    FirstOrDefault(x=> x.index == ObjectsBoard[(int) (room.xMax - 2), i]);
                                if (prefab != null)
                                    Instantiate(prefab.rightGameObject, 
                                        new Vector3((int) (room.xMax - 2), 0, i) * 4 + prefab.offset, 
                                        Quaternion.identity, parent);
                            }
                        }
                    }

                    break;
                
                case CorridorDirection.Right:
                    
                    for (int i = (int) (room.yMin + 1); i < (room.yMax - 1); i++)
                    {
                        // Instantiate(OccupiedGO, new Vector3((int) room.xMin,0,i) * 4, Quaternion.identity, parent);

                        if (DungeonBoard[(int) room.xMin, i] != TileType.Hallway)
                        {
                            if ((PhotonNetwork.IsConnectedAndReady && PhotonNetwork.IsMasterClient) || isHost)
                            {
                                Random random = new Random();
                                var sidePrefab = sidePrefabList[random.Next(0, sidePrefabList.Count)];
                                var sideOffset = sidePrefab.offset;
                                ObjectsBoard[(int) (room.xMin + 1), i] = sidePrefab.index;

                                var position = new Vector3((int) (room.xMin + 1), 0, i);
                                Instantiate(sidePrefab.rightGameObject, position * 4 + sideOffset, Quaternion.identity, parent);
                                UpdateObjectsRadius(position, RoomAvailability.Occupied, sidePrefab.radius);
                            }

                            else
                            {
                                var prefab = kitchenRoom.
                                    FirstOrDefault(x=> x.index == ObjectsBoard[(int) (room.xMin + 1), i]);
                                if (prefab != null)
                                    Instantiate(prefab.rightGameObject, 
                                        new Vector3((int) (room.xMin + 1), 0, i) * 4 + prefab.offset, 
                                        Quaternion.identity, parent);
                            }
                        }
                    }
                
                    break;
                
                case CorridorDirection.Top:
                    
                    for (int i = (int) (room.xMin + 1); i < (room.xMax - 1); i++)
                    {
                        // Instantiate(OccupiedGO, new Vector3(i, 0, (int) room.yMin) * 4, Quaternion.identity, parent);

                        if (DungeonBoard[i, (int) room.yMin] != TileType.Hallway)
                        {
                            if ((PhotonNetwork.IsConnectedAndReady && PhotonNetwork.IsMasterClient) || isHost)
                            {
                                Random random = new Random();
                                var sidePrefab = sidePrefabList[random.Next(0, sidePrefabList.Count)];
                                var sideOffset = sidePrefab.offset;
                                ObjectsBoard[i, (int) (room.yMin + 1)] = sidePrefab.index;
                            
                                var position = new Vector3(i, 0, (int) (room.yMin + 1));
                                Instantiate(sidePrefab.gameObject, position * 4 + sideOffset, Quaternion.identity, parent);
                                UpdateObjectsRadius(position, RoomAvailability.Occupied, sidePrefab.radius);
                            }
                            
                            else
                            {
                                var prefab = kitchenRoom.
                                    FirstOrDefault(x=> x.index == ObjectsBoard[i, (int) (room.yMin + 1)]);
                                if (prefab != null)
                                    Instantiate(prefab.gameObject, 
                                        new Vector3(i, 0, (int) (room.yMin + 1)) * 4 + prefab.offset, 
                                        Quaternion.identity, parent);
                            }
                        }
                    }
                    
                    break;
                
                case CorridorDirection.Bottom:
                    
                    for (int i = (int) (room.xMin + 1); i < (room.xMax - 1); i++)
                    {
                        // Instantiate(OccupiedGO, new Vector3(i, 0, (int) room.yMax - 1) * 4, Quaternion.identity, parent);
                        
                        if (DungeonBoard[i, (int) room.yMax - 1] != TileType.Hallway)
                        {

                            if ((PhotonNetwork.IsConnectedAndReady && PhotonNetwork.IsMasterClient) || isHost)
                            {
                                Random random = new Random();
                                var sidePrefab = sidePrefabList[random.Next(0, sidePrefabList.Count)];
                                var sideOffset = sidePrefab.offset;
                                ObjectsBoard[i, (int) (room.yMax - 2)] = sidePrefab.index;

                            
                                var position = new Vector3(i, 0, (int) (room.yMax - 2));
                                Instantiate(sidePrefab.gameObject, position * 4 + sideOffset, Quaternion.identity, parent);
                                UpdateObjectsRadius(position, RoomAvailability.Occupied, sidePrefab.radius);
                            }
                            
                            else
                            {
                                var prefab = kitchenRoom.
                                    FirstOrDefault(x=> x.index == ObjectsBoard[i, (int) (room.yMax - 2)]);
                                if (prefab != null)
                                    Instantiate(prefab.gameObject, 
                                        new Vector3(i, 0, (int) (room.yMax - 2)) * 4 + prefab.offset, 
                                        Quaternion.identity, parent);
                            }
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
            if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.IsMasterClient || !isHost) return;

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
                    ObjectsBoard[i, j] = objectPrefab.index;

                    Instantiate(objectPrefab.gameObject, position * 4, Quaternion.identity, parent);
                    UpdateObjectsRadius(position, RoomAvailability.Occupied, objectPrefab.radius);
                    // Instantiate(occupiedGO, position * 4, Quaternion.identity, parent);
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
            roomAvailability = new RoomAvailability[boardWidth, boardHeight];

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

            if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.IsMasterClient || !isHost) DrawObjectsNonHost();
        }


        private void DrawObjectsNonHost()
        {
            for (int i = 0; i < boardWidth; i++)
            {
                for (int j = 0; j < boardHeight; j++)
                {
                    if (ObjectsBoard[i, j] == 0) continue;

                    var position = new Vector3(i, 0, j);

                    DungeonObject dungeonObject = genericRoom.FirstOrDefault(x => x.index == ObjectsBoard[i, j]);
                    if (dungeonObject != default)
                        Instantiate(dungeonObject.gameObject, position * 4, Quaternion.identity, parent);

                    dungeonObject = skeletonRoom.FirstOrDefault(x => x.index == ObjectsBoard[i, j]);
                    if (dungeonObject != default)
                        Instantiate(dungeonObject.gameObject, position * 4 + dungeonObject.offset, Quaternion.identity,
                            parent);
                }
            }
        }
        
        
        #endregion
    }
}
