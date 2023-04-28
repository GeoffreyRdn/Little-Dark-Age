using UnityEngine;

namespace Dungeon
{
    public class GetDungeon : MonoBehaviour
    {
        #region Methods
        internal void GetData(SubDungeon dungeon, Generation generation)
        {
            GetRooms(dungeon, generation);
            GetHallways(dungeon, generation);
        }
        private void GetRooms(SubDungeon dungeon, Generation generation)
        {
            if (dungeon == null) return;
            
            if (dungeon.IsLeaf())
            {
                for (int i = (int) dungeon.Room.x + 1; i < (int) dungeon.Room.xMax - 1; i++)
                {
                    for (int j = (int) dungeon.Room.y + 1; j < dungeon.Room.yMax - 1; j++)
                    {
                        // by default tile is a Room
                        generation.DungeonBoard[i, j] = TileType.Room;
                        
                        // surrounds the tile by walls if not already room / hallway
                        for (int k = i - 1; k <= i + 1; k++)
                        {
                            for (int l = j - 1; l <= j + 1; l++)
                            {
                                if (generation.DungeonBoard[k, l] != TileType.Room && 
                                    generation.DungeonBoard[k, l] != TileType.Hallway)
                                {
                                    generation.DungeonBoard[k, l] = TileType.Wall;
                                }
                            }
                        }
                    }
                }
                
                generation.AddRoom(dungeon.Room);
            }
            else
            {
                GetRooms(dungeon.LeftChild, generation);
                GetRooms(dungeon.RightChild, generation);
            }
        }
        private void GetHallways(SubDungeon dungeon, Generation generation)
        {
            if (dungeon == null) return;

            // get all the hallways
            GetHallways(dungeon.LeftChild, generation);
            GetHallways(dungeon.RightChild, generation);

            // when we reach the bottom, we go back at the first room and getting the hallways
            foreach (var h in dungeon.Hallway)
            {
                for (int i = (int) h.x; i < (int) h.xMax; i++)
                {
                    for (int j = (int) h.y; j < (int) h.yMax; j++)
                    {
                        // if already a room we leave it that way 
                        // otherwise it is a hallway
                        generation.DungeonBoard[i, j] = generation.DungeonBoard[i, j] == TileType.Room 
                            ? TileType.Room 
                            : TileType.Hallway;

                        // creating walls around the hallways
                        for (int k = i - 1; k <= i + 1; k++)
                        {
                            for (int l = j - 1; l <= j + 1; l++)
                            {
                                if (generation.DungeonBoard[k, l] == TileType.Void)
                                    generation.DungeonBoard[k, l] = TileType.Wall;
                            }
                        }
                    }
                }
            }
        }
        
        #endregion
    }
}
