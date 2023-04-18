using UnityEngine;

namespace Dungeon
{
    public class BSP : MonoBehaviour
    {
        #region Methods
        internal static void BinarySpacePartitioning(SubDungeon dungeon, Generation generation)
        {
            // while dungeon's rooms are too big we split them
            if (dungeon.IsLeaf() &&
                dungeon.Rectangle.width * dungeon.Rectangle.height > generation.maxRoomSize
                && dungeon.Split(generation.minRoomSize))
            {
                BinarySpacePartitioning(dungeon.LeftChild, generation);
                BinarySpacePartitioning(dungeon.RightChild, generation);
            }
        }
        
        #endregion
    }
}
