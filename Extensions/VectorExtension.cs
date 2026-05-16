using UnityEngine;

namespace JxModule
{
    public static class VectorExtension
    {
        public static Vector3 GetDirection(this Vector3 from, Vector3 to)
        {
            return (to - from).normalized;
        }

        public static Vector2 GetDirection2D(this Vector3 from, Vector3 to)
        {
            var direction = to - from;
            return new Vector3(direction.x, direction.y).normalized;
        }

        public static Vector2 GetDirection2D(this Vector2 from, Vector2 to)
        {
            return (to - from).normalized;
        }
    }
}