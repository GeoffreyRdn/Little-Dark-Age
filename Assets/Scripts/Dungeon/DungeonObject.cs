using UnityEngine;

namespace Dungeon
{
    [System.Serializable]
    public class DungeonObject
    {
        public GameObject gameObject;
        public int count;
        public int radius;
        public ObjectPosition position;
        public Vector3 offset;
    }

    public enum ObjectPosition
    {
        AllSides,
        RandomSide,
        NotCorridorSideAndOpposite,
        CorridorSideAndOpposite,
        Everywhere,
        Middle
    }
}