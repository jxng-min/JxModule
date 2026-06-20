#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace JxModule
{
    public class ObjectMonitorWindow : EditorWindow
    {
        private Vector2 _scroll;
        
        private readonly float[] _widths =
        {
            180f, 90f, 90f, 90f, 90f, 90f, 90f, 90f
        };
        
        private float TotalWidth => _widths.Sum();

        private readonly string[] _headers =
        {
            "Name", "Active", "Pooled", "Total", "Peak", "Average", "Created", "Expanded"
        };

        [MenuItem("JxModule/Object Pool Monitor")]
        private static void Open()
        {
            var window = GetWindow<ObjectMonitorWindow>("Object Pool Profiler");
            window.minSize = new Vector2(810f, 150f);
        }

        private void OnGUI()
        {
            DrawTableHeader();
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("프로파일링하려면 플레이 모드를 실행해주세요.", MessageType.Info);
                return;
            }

            var manager = FindFirstObjectByType<ObjectPoolManager>();
            if (manager == null)
            {
                EditorGUILayout.HelpBox("프로파일링할 오브젝트 풀이 없습니다.", MessageType.Error);
                return;
            }
            
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            var rowIndex = 0;
            foreach (var pair in manager.Pools)
            {
                if (pair.Value == null)
                    continue;

                DrawPoolRow(pair.Value, rowIndex);
                rowIndex++;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawTableHeader()
        {
            var rect = GUILayoutUtility.GetRect(TotalWidth, 30f);
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));

            var x = rect.x;

            for (var i = 0; i < _headers.Length; i++)
            {
                var cellRect = new Rect(x, rect.y, _widths[i], rect.height);
                var paddedRect = new Rect(cellRect.x + 6f, cellRect.y + 2f, cellRect.width - 8f, cellRect.height);
                
                GUI.Label(paddedRect, _headers[i], EditorStyles.boldLabel);
                x += _widths[i];
            }
        }

        private void DrawPoolRow(JxObjectPool pool, int rowIndex)
        {
            var rect = GUILayoutUtility.GetRect(TotalWidth, 30f);

            var bgColor = rowIndex % 2 == 0
                ? new Color(0.18f, 0.18f, 0.18f)
                : new Color(0.15f, 0.15f, 0.15f);

            EditorGUI.DrawRect(rect, bgColor);

            var values = new string[]
            {
                pool.PoolName,
                pool.ActiveCount.ToString(),
                pool.PooledCount.ToString(),
                pool.TotalCount.ToString(),
                pool.PeakActiveCount.ToString(),
                pool.AverageActiveCount.ToString("F1"),
                pool.CreatedCount.ToString(),
                pool.ExpandedCreateCount.ToString()
            };

            var x = rect.x;

            for (var i = 0; i < values.Length; i++)
            {
                var cellRect = new Rect(x, rect.y, _widths[i], rect.height);
                var paddedRect = new Rect(cellRect.x + 6f, cellRect.y + 2f, cellRect.width - 8f, cellRect.height);

                GUI.Label(paddedRect, values[i]);
                x += _widths[i];
            }
        }

        private void Update()
        {
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
    }
}
#endif