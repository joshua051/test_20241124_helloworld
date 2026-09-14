using System;

namespace UnityEngine
{
    public static class Mathf
    {
        public const float Deg2Rad = MathF.PI / 180f;
        public static float Cos(float value) => MathF.Cos(value);
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 forward => new(0f, 0f, 1f);
        public static Vector3 back => new(0f, 0f, -1f);
        public float sqrMagnitude => x * x + y * y + z * z;
        public float magnitude => MathF.Sqrt(sqrMagnitude);
        public Vector3 normalized
        {
            get
            {
                float mag = magnitude;
                return mag <= 0.000001f ? new Vector3(0f, 0f, 0f) : new Vector3(x / mag, y / mag, z / mag);
            }
        }
        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
    }
}
