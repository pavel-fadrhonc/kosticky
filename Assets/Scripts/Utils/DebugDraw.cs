using UnityEngine;

namespace DefaultNamespace.Utils
{
    public static class DebugDraw
    {
        public static void DrawBounds(Bounds b, float delay=0)
        {
            // bottom
            var p1 = new Vector3(b.min.x, b.min.y, b.min.z);
            var p2 = new Vector3(b.max.x, b.min.y, b.min.z);
            var p3 = new Vector3(b.max.x, b.min.y, b.max.z);
            var p4 = new Vector3(b.min.x, b.min.y, b.max.z);

            Debug.DrawLine(p1, p2, Color.blue, delay);
            Debug.DrawLine(p2, p3, Color.red, delay);
            Debug.DrawLine(p3, p4, Color.yellow, delay);
            Debug.DrawLine(p4, p1, Color.magenta, delay);

            // top
            var p5 = new Vector3(b.min.x, b.max.y, b.min.z);
            var p6 = new Vector3(b.max.x, b.max.y, b.min.z);
            var p7 = new Vector3(b.max.x, b.max.y, b.max.z);
            var p8 = new Vector3(b.min.x, b.max.y, b.max.z);

            Debug.DrawLine(p5, p6, Color.blue, delay);
            Debug.DrawLine(p6, p7, Color.red, delay);
            Debug.DrawLine(p7, p8, Color.yellow, delay);
            Debug.DrawLine(p8, p5, Color.magenta, delay);

            // sides
            Debug.DrawLine(p1, p5, Color.white, delay);
            Debug.DrawLine(p2, p6, Color.gray, delay);
            Debug.DrawLine(p3, p7, Color.green, delay);
            Debug.DrawLine(p4, p8, Color.cyan, delay);
        }
        
        
        public static void Sphere(Vector3 center, float radius)
        {
            Sphere(center, radius, Color.white);
        }

        public static void Sphere(Vector3 center, float radius, Color color, float duration = 0)
        {
            CircleInternal(center, Vector3.right, Vector3.up, radius, color, duration);
            CircleInternal(center, Vector3.forward, Vector3.up, radius, color, duration);
            CircleInternal(center, Vector3.right, Vector3.forward, radius, color, duration);
        }
        
        static void CircleInternal(Vector3 center, Vector3 v1, Vector3 v2, float radius, Color color, float duration = 0)
        {
            const int segments = 20;
            float arc = Mathf.PI * 2.0f / segments;
            Vector3 p1 = center + v1 * radius;
            for (var i = 1; i <= segments; i++)
            {
                Vector3 p2 = center + v1 * Mathf.Cos(arc * i) * radius + v2 * Mathf.Sin(arc * i) * radius;
                Debug.DrawLine(p1, p2, color, duration);
                p1 = p2;
            }
        }
    }
}