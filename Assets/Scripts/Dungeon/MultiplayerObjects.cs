using System;



namespace Dungeon
{
    public static class MultiplayerObjects
    {
        public static int[][] Array2dToArrayArray<T>(this T[,] mapData) where T : Enum
        {
            int[][] d = new int[mapData.GetLength(0)][];
            for (int i = 0; i < d.Length; i++)
                d[i] = new int[mapData.GetLength(1)];

            for (int i = 0; i < mapData.GetLength(0); i++)
                for (int j = 0; j < mapData.GetLength(1); j++)
                    d[i][j] = Convert.ToInt32(mapData[i, j]);

            return d;
        }

        public static TileType[,] ArrayArrayToArray2d(this int[][] mapData)
        {
            TileType[,] d = new TileType[mapData.Length,mapData[0].Length];
            
            for (int i = 0; i < mapData.Length; i++)
                for (int j = 0; j < mapData[0].Length; j++)
                    d[i, j] = (TileType) mapData[i][j];
            
            return d;
        }
    }
}
