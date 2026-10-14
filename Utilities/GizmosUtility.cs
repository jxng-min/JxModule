
using System.Collections.Generic;
using UnityEngine;

namespace JxModule
{
    public static class GizmosUtility
    {
        public static void DrawGrid(Vector3 origin,
                                    int width,
                                    int height,
                                    float cellSize,
                                    Color color,
                                    Matrix4x4 matrix = default)
        {
            if (width <= 0 || height <= 0 || cellSize <= 0f)
            {
                return;
            }

            var previousColor = Gizmos.color;
            var previousMatrix = Gizmos.matrix;

            Gizmos.color = color;
            Gizmos.matrix = matrix == default ? Matrix4x4.identity : matrix;

            for (var x = 0; x <= width; x++)
            {
                var start = origin + Vector3.right * (x * cellSize);
                var end = start + Vector3.forward * (height * cellSize);

                Gizmos.DrawLine(start, end);
            }

            for (var y = 0; y <= height; y++)
            {
                var start = origin + Vector3.forward * (y * cellSize);
                var end = start + Vector3.right * (width * cellSize);

                Gizmos.DrawLine(start, end);
            }

            Gizmos.color = previousColor;
            Gizmos.matrix = previousMatrix;
        }

        public static void DrawCell(Vector3 center,
                                    float cellSize,
                                    Color color,
                                    Matrix4x4 matrix = default,
                                    float scale = 0.9f)
        {
            if (cellSize <= 0f)
            {
                return;
            }

            var previousColor = Gizmos.color;
            var previousMatrix = Gizmos.matrix;

            Gizmos.color = color;
            Gizmos.matrix = matrix == default ? Matrix4x4.identity : matrix;
            Gizmos.DrawCube(center, new Vector3(cellSize * scale, 0.05f, cellSize * scale));
            Gizmos.color = previousColor;
            Gizmos.matrix = previousMatrix;
        }

        public static void DrawPath(IReadOnlyList<Vector3> points, Color color, float pointRadius = 0.1f, bool drawPoints = true)
        {
            if (points == null || points.Count == 0)
            {
                return;
            }

            var previousColor = Gizmos.color;
            Gizmos.color = color;

            for (var i = 0; i < points.Count; i++)
            {
                if (drawPoints)
                {
                    Gizmos.DrawSphere(points[i], pointRadius);
                }

                if (i > 0)
                {
                    Gizmos.DrawLine(points[i - 1], points[i]);
                }
            }

            Gizmos.color = previousColor;
        }

        public static void DrawPoint(Vector3 position, float radius, Color color)
        {
            if (radius <= 0f)
            {
                return;
            }

            var previousColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawSphere(position, radius);
            Gizmos.color = previousColor;
        }

        public static void DrawArrow(Vector3 origin,
                                     Vector3 direction,
                                     float length,
                                     Color color,
                                     float headLength = 0.25f,
                                     float headAngle = 25f)
        {
            if (direction.sqrMagnitude < 0.000001f || length <= 0f)
            {
                return;
            }

            var previousColor = Gizmos.color;
            Gizmos.color = color;

            var normalized = direction.normalized;
            var end = origin + normalized * length;

            Gizmos.DrawLine(origin, end);

            var axis = Vector3.Cross(normalized, Vector3.up);

            if (axis.sqrMagnitude < 0.000001f)
            {
                axis = Vector3.Cross(normalized, Vector3.right);
            }

            axis.Normalize();

            var left = Quaternion.AngleAxis(headAngle, axis) * -normalized;
            var right = Quaternion.AngleAxis(-headAngle, axis) * -normalized;

            Gizmos.DrawLine(end, end + left * headLength);
            Gizmos.DrawLine(end, end + right * headLength);

            Gizmos.color = previousColor;
        }

        public static void DrawCircle(Vector3 center,
                                      float radius,
                                      Color color,
                                      int segments = 32,
                                      Vector3 normal = default)
        {
            if (radius <= 0f || segments < 3)
            {
                return;
            }

            var previousColor = Gizmos.color;
            Gizmos.color = color;

            if (normal.sqrMagnitude < 0.000001f)
            {
                normal = Vector3.up;
            }

            var rotation = Quaternion.FromToRotation(Vector3.up, normal.normalized);

            var previous = center + rotation * Vector3.forward * radius;

            for (var i = 1; i <= segments; i++)
            {
                var angle = i * Mathf.PI * 2f / segments;
                var local = new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
                var current = center + rotation * local;

                Gizmos.DrawLine(previous, current);
                previous = current;
            }

            Gizmos.color = previousColor;
        }

        public static void DrawLabel(Vector3 position, string text, Color color)
        {
#if UNITY_EDITOR
            var previousColor = UnityEditor.Handles.color;

            try
            {
                UnityEditor.Handles.color = color;
                UnityEditor.Handles.Label(position, text);
            }
            finally
            {
                UnityEditor.Handles.color = previousColor;
            }
#endif
        }
    }
}
