using UnityEngine;

namespace DefaultNamespace
{
    public class VoxelConsts
    {
        public static readonly Vector3[] vertexPos =  
        {
            // bottom quad cc-wise
            new Vector3(0f, 0f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(1f, 0f, 1f),
            new Vector3(0f, 0f, 1f),

            // top quad cc-wise
            new Vector3(0f, 1f, 0f),
            new Vector3(1f, 1f, 0f),
            new Vector3(1f, 1f, 1f),
            new Vector3(0f, 1f, 1f),
        };

        public static readonly int[,] faces =
        {
            {0, 4, 1, 1, 4, 5}, // front
            {2, 6, 3, 3, 6, 7}, // back
            {1, 5, 2, 2, 5, 6}, // right
            {3, 7, 0, 0, 7, 4}, // left
            {3, 0, 2, 2, 0, 1}, // bottom
            {4, 7, 5, 5, 7, 6} // top
        };

        public static readonly Vector2[] uvs =
        {
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
        };
    }

    public enum EVoxelFaceType
    {
        Front       = 0,
        Back        = 1,
        Right       = 2,
        Left        = 3,
        Bottom      = 4,
        Top         = 5
    }
}
