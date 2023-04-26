using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Dungeon
{
    public class SubDungeon
    {
        #region Variables
        
        public SubDungeon LeftChild;
        public SubDungeon RightChild;
        
        public Rect Rectangle;
        public Rect Room;
        
        public readonly List<Rect> Hallway = new ();
        private readonly int minCorridor;
        private readonly int maxCorridor;

        #endregion

        #region Class
        
        public SubDungeon(Rect rectangle, int minCorridor, int maxCorridor)
        {
            // parent has no children
            LeftChild = null;
            RightChild = null;
            
            Rectangle = rectangle;
            
            this.minCorridor = minCorridor;
            this.maxCorridor = maxCorridor;

            Room = new Rect(0, 0, 0, 0); // initialisation to null
        }

        #endregion

        #region Methods
        
        internal bool IsLeaf()
            => LeftChild == null && RightChild == null;

        internal bool Split(int minRoomSize)
        {
            if (!IsLeaf()) return false;

            bool horizontalSplit = !(Rectangle.width / Rectangle.height >= 1.1);  // choose the split dir

            if (Mathf.Min(Rectangle.height, Rectangle.width) / 2 < minRoomSize)
                return false; // stop the split bcs the room is small enough already

            if (horizontalSplit)
            {
                // choose a random y to split horizontally
                int split = Random.Range(minRoomSize, (int) (Rectangle.width - minRoomSize));

                // bottom part of the rectangle
                LeftChild = new SubDungeon(new Rect(Rectangle.x, Rectangle.y, Rectangle.width, split), 
                    minCorridor, maxCorridor);
                // top part of the rectangle
                RightChild = new SubDungeon(new Rect(Rectangle.x, Rectangle.y + split, Rectangle.width,
                    Rectangle.height - split), minCorridor, maxCorridor);
            }
            else // same as horizontal split 
            {
                // choose a random x to split vertically
                int split = Random.Range(minRoomSize, (int) (Rectangle.height - minRoomSize));

                // left part of the rectangle
                LeftChild = new SubDungeon(new Rect(Rectangle.x, Rectangle.y, split, Rectangle.height),
                    minCorridor, maxCorridor);
                // right part of the rectangle
                RightChild = new SubDungeon(new Rect(Rectangle.x + split, Rectangle.y, Rectangle.width - split,
                    Rectangle.height), minCorridor, maxCorridor);
            }

            return true;
        }

        private Rect RoomCreation()
        {
            if (IsLeaf()) return Room;

            if (LeftChild != null)
            {
                Rect leftRoom = LeftChild.RoomCreation();
                if (Math.Abs(leftRoom.x + 1) > .1) return leftRoom;
            }

            if (RightChild != null)
            {
                Rect rightRoom = RightChild.RoomCreation();
                if (Math.Abs(rightRoom.x + 1) > .1) return rightRoom;
            }

            return new Rect(0, 0, 0, 0); // null
        }

        private void CreateHallways(Rect leftRoom, Rect rightRoom)
        {
            Vector2 leftPoint = new Vector2((int) Random.Range(leftRoom.x + 2, leftRoom.xMax - 2),
                (int) Random.Range(leftRoom.y + 1, leftRoom.yMax - 1));
            Vector2 rightPoint = new Vector2((int) Random.Range(rightRoom.x + 2, rightRoom.xMax - 2),
                (int) Random.Range(rightRoom.y + 2, rightRoom.yMax - 2));

            if (leftPoint.x > rightPoint.x) (leftPoint, rightPoint) = (rightPoint, leftPoint);

            int width = (int) (leftPoint.x - rightPoint.x);
            int height = (int) (leftPoint.y - rightPoint.y);

            int corridorSize = Random.Range(minCorridor, maxCorridor);

            if (width != 0) // not aligned
            {
                if (Random.Range(0, 1) > 0.5f) // horizontal or vertical path
                {
                    Hallway.Add(new Rect(leftPoint.x, leftPoint.y, Mathf.Abs(width) + 1, corridorSize)); // right

                    Hallway.Add(height < 0
                        ? new Rect(rightPoint.x, leftPoint.y, corridorSize, Mathf.Abs(height))
                        : new Rect(rightPoint.x, leftPoint.y, corridorSize, -Mathf.Abs(height)));
                }
                else
                {
                    Hallway.Add(height < 0
                        ? new Rect(leftPoint.x, leftPoint.y, corridorSize, Mathf.Abs(height))
                        : new Rect(leftPoint.x, rightPoint.y, corridorSize, Mathf.Abs(height)));

                    Hallway.Add(new Rect(leftPoint.x, rightPoint.y, Mathf.Abs(width) + 1, corridorSize)); // right
                }
            }
            else // aligned
            {
                if (height < 0)
                    Hallway.Add(new Rect((int) leftPoint.x, (int) leftPoint.y, corridorSize,
                        Mathf.Abs(height))); // up
                else
                    Hallway.Add(new Rect((int) rightPoint.x, (int) rightPoint.y, corridorSize,
                        Mathf.Abs(height))); // down
            }
        }

        internal void GetRoom()
        {
            LeftChild?.GetRoom(); // create if != null
            RightChild?.GetRoom();

            if (LeftChild != null && RightChild != null) CreateHallways(LeftChild.RoomCreation(), RightChild.RoomCreation());

            if (IsLeaf())
            {
                int width = (int) Random.Range(Rectangle.width / 2,
                    Rectangle.width - 5); // rectangle.width - x --> x can be adjusted (size in the rectangle)
                int height = (int) Random.Range(Rectangle.height / 2, Rectangle.height - 5); // same
                int x = (int) Random.Range(1, Rectangle.width - width);
                int y = (int) Random.Range(1, Rectangle.height - height);

                // room position will be absolute in the board, not relative to the sub-dungeon
                Room = new Rect(Rectangle.x + x, Rectangle.y + y, width, height);
            }
        }
        
        #endregion
    }
}
