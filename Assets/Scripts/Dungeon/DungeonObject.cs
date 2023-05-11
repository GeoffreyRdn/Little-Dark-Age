using UnityEngine;

namespace Dungeon
{
    [System.Serializable]
    public class DungeonObject
    {
        public GameObject gameObject;
        public GameObject rightGameObject;
        public GameObject leftGameObject;
        public GameObject bottomGameObject;
        
        public int radius;
        public ObjectPosition position;
        public Vector3 offset;
        
        public int index;
    }

    public enum ObjectPosition
    {
        Everywhere,
        Side,
        Middle
    }
}