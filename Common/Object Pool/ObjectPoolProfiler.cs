#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace JxModule
{
    public class ObjectPoolProfiler : EditorWindow
    {
        private const float DefaultGraphHeight = 320f;
        private const float MinGraphHeight = 200f;
        private const float MaxGraphHeight = 1000f;
        private const float GraphZoomSpeed = 30f;
        
        private const float GraphPaddingLeft = 40f;
        private const float GraphPaddingRight = 15f;
        private const float GraphPaddingTop = 15f;
        private const float GraphPaddingBottom = 25f;
        
        private Vector2 _scrollPosition;
        private float _graphHeight = DefaultGraphHeight;
        
        private JxObjectPool _selectedPool;
        
        private readonly Dictionary<JxObjectPool, bool> _visibleStates = new();
        private readonly Dictionary<JxObjectPool, Color> _poolColors = new();
        
        [MenuItem("JxModule/Object Pool/Object Pool Profiler")]
        private static void Open()
        {
            var window = GetWindow<ObjectPoolProfiler>("Object Pool Profiler");
            window.minSize = new Vector2(850f, 500f);
        }
        
        private void OnGUI()
        {
            DrawToolbar();
            
            EditorGUILayout.Space(4f);
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Object Pool Profiler is available in Play Mode.", MessageType.Info);
                return;
            }
            
            var manager = FindFirstObjectByType<ObjectPoolManager>();
            
            if (manager == null)
            {
                EditorGUILayout.HelpBox("ObjectPoolManager could not be found.", MessageType.Warning);
                return;
            }
            
            RefreshPoolStates(manager);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            DrawVisiblePools(manager);
            
            EditorGUILayout.Space(6f);
            
            DrawGraph(manager);
            
            EditorGUILayout.Space(8f);
            
            if (_selectedPool != null)
            {
                DrawSelectedPool(_selectedPool);
                
                EditorGUILayout.Space(8f);
                
                DrawActiveObjects(_selectedPool);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Object Pool Profiler", EditorStyles.boldLabel);
                
                GUILayout.FlexibleSpace();
                
                GUI.enabled = Application.isPlaying;
                
                if (GUILayout.Button("Show All", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                {
                    SetAllVisible(true);
                }
                
                if (GUILayout.Button("Hide All", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                {
                    SetAllVisible(false);
                }
                
                if (GUILayout.Button("Reset Graph", EditorStyles.toolbarButton, GUILayout.Width(85f)))
                {
                    _graphHeight = DefaultGraphHeight;
                }
                
                if (GUILayout.Button("Reset All", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                {
                    ResetAll();
                }
                
                GUI.enabled = true;
            }
        }
        
        private void DrawVisiblePools(ObjectPoolManager manager)
        {
            EditorGUILayout.LabelField("Visible Pools", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var pools = manager.Pools
                    .Values
                    .Where(pool => pool != null)
                    .OrderBy(pool => pool.PoolName)
                    .ToList();
                
                const int columnCount = 4;
                
                for (var i = 0; i < pools.Count; i += columnCount)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        for (var j = 0; j < columnCount; j++)
                        {
                            var index = i + j;
                            
                            if (index >= pools.Count)
                            {
                                GUILayout.FlexibleSpace();
                                continue;
                            }
                            
                            DrawPoolLegendItem(pools[index]);
                        }
                    }
                }
            }
        }
        
        private void DrawPoolLegendItem(JxObjectPool pool)
        {
            using (new EditorGUILayout.HorizontalScope(GUILayout.MinWidth(180f)))
            {
                var color = GetPoolColor(pool);
                
                var colorRect = GUILayoutUtility.GetRect(10f, 10f, GUILayout.Width(10f));
                colorRect.y += 3f;
                
                EditorGUI.DrawRect(colorRect, color);
                
                var visible = GetVisibleState(pool);
                var newVisible = GUILayout.Toggle(visible, GUIContent.none, GUILayout.Width(18f));
                
                if (newVisible != visible)
                {
                    _visibleStates[pool] = newVisible;
                }
                
                var previousColor = GUI.contentColor;
                
                if (_selectedPool == pool)
                {
                    GUI.contentColor = color;
                }
                
                if (GUILayout.Button(pool.PoolName, EditorStyles.label))
                {
                    _selectedPool = pool;
                }
                
                GUI.contentColor = previousColor;
            }
        }
        
        private void DrawGraph(ObjectPoolManager manager)
        {
            EditorGUILayout.LabelField("Active Count History", EditorStyles.boldLabel);
            
            var graphRect = GUILayoutUtility.GetRect(position.width - 30f, _graphHeight);
            
            HandleGraphZoom(graphRect);
            
            EditorGUI.DrawRect(graphRect, new Color(0.11f, 0.11f, 0.11f));
            
            GUI.Label(
                new Rect(graphRect.xMax - 100f, graphRect.y + 4f, 90f, 18f),
                $"{_graphHeight:F0}px",
                EditorStyles.miniLabel
            );
            
            var contentRect = new Rect(
                graphRect.x + GraphPaddingLeft,
                graphRect.y + GraphPaddingTop,
                graphRect.width - GraphPaddingLeft - GraphPaddingRight,
                graphRect.height - GraphPaddingTop - GraphPaddingBottom
            );
            
            var visiblePools = manager.Pools
                .Values
                .Where(pool => pool != null && GetVisibleState(pool))
                .ToList();
            
            if (visiblePools.Count == 0)
            {
                GUI.Label(graphRect, "No visible pools.", EditorStyles.centeredGreyMiniLabel);
                
                return;
            }
            
            var maxValue = GetGraphMaxValue(visiblePools);
            
            DrawGraphGrid(contentRect, maxValue);
            
            Handles.BeginGUI();
            
            foreach (var pool in visiblePools)
            {
                DrawPoolGraph(contentRect, pool, maxValue);
            }
            
            Handles.EndGUI();
        }
        
        private void HandleGraphZoom(Rect graphRect)
        {
            var currentEvent = Event.current;
            
            if (!graphRect.Contains(currentEvent.mousePosition))
            {
                return;
            }
            
            if (currentEvent.type == EventType.ScrollWheel)
            {
                _graphHeight -= currentEvent.delta.y * GraphZoomSpeed;
                _graphHeight = Mathf.Clamp(_graphHeight, MinGraphHeight, MaxGraphHeight);
                
                currentEvent.Use();
                Repaint();
                
                return;
            }
            
            if (currentEvent.type == EventType.MouseDown &&
                currentEvent.button == 0 &&
                currentEvent.clickCount == 2)
            {
                _graphHeight = DefaultGraphHeight;
                
                currentEvent.Use();
                Repaint();
            }
        }
        
        private void DrawGraphGrid(Rect rect, int maxValue)
        {
            const int horizontalLineCount = 5;
            
            Handles.BeginGUI();
            
            var previousColor = Handles.color;
            
            Handles.color = new Color(1f, 1f, 1f, 0.1f);
            
            for (var i = 0; i <= horizontalLineCount; i++)
            {
                var normalized = (float)i / horizontalLineCount;
                var y = Mathf.Lerp(rect.yMax, rect.yMin, normalized);
                
                Handles.DrawLine(new Vector3(rect.xMin, y), new Vector3(rect.xMax, y));
                
                var value = Mathf.RoundToInt(maxValue * normalized);
                GUI.Label(new Rect(rect.xMin - 38f, y - 8f, 35f, 16f), value.ToString(), EditorStyles.miniLabel);
            }
            
            Handles.color = new Color(1f, 1f, 1f, 0.2f);
            Handles.DrawLine(new Vector3(rect.xMin, rect.yMin), new Vector3(rect.xMin, rect.yMax));
            Handles.DrawLine(new Vector3(rect.xMin, rect.yMax), new Vector3(rect.xMax, rect.yMax));
            Handles.color = previousColor;
            Handles.EndGUI();
            
            GUI.Label(new Rect(rect.xMax - 75f, rect.yMax + 4f, 75f, 18f), "Recent 30s", EditorStyles.miniLabel);
        }
        
        private void DrawPoolGraph(Rect rect, JxObjectPool pool, int maxValue)
        {
            var history = pool.ActiveCountHistory.ToArray();
            
            if (history.Length < 2)
            {
                return;
            }
            
            var points = new Vector3[history.Length];
            
            for (var i = 0; i < history.Length; i++)
            {
                var normalizedX = (float)i / (history.Length - 1);
                var normalizedY = maxValue <= 0 ? 0f : (float)history[i] / maxValue;
                
                points[i] = new Vector3(
                    Mathf.Lerp(rect.xMin, rect.xMax, normalizedX),
                    Mathf.Lerp(rect.yMax, rect.yMin, normalizedY)
                );
            }
            
            var previousColor = Handles.color;
            Handles.color = GetPoolColor(pool);
            Handles.DrawAAPolyLine(_selectedPool == pool ? 3f : 2f, points);
            Handles.color = previousColor;
        }
        
        private int GetGraphMaxValue(List<JxObjectPool> pools)
        {
            var maxValue = 1;
            
            foreach (var pool in pools)
            {
                foreach (var value in pool.ActiveCountHistory)
                {
                    maxValue = Mathf.Max(maxValue, value);
                }
                
                maxValue = Mathf.Max(maxValue, pool.InitialPoolSize);
            }
            
            maxValue = Mathf.CeilToInt(maxValue * 1.1f);
            
            return Mathf.Max(1, maxValue);
        }
        
        private void DrawSelectedPool(JxObjectPool pool)
        {
            var color = GetPoolColor(pool);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var colorRect = GUILayoutUtility.GetRect(12f, 12f, GUILayout.Width(12f));
                    colorRect.y += 3f;
                    
                    EditorGUI.DrawRect(colorRect, color);
                    GUILayout.Label(pool.PoolName, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    
                    var previousColor = GUI.contentColor;
                    GUI.contentColor = GetStatusColor(pool.Status);
                    GUILayout.Label(GetStatusText(pool.Status), EditorStyles.boldLabel);
                    GUI.contentColor = previousColor;
                }
                
                EditorGUILayout.Space(4f);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawValue("Current", pool.ActiveCount.ToString());
                    DrawValue("Peak", pool.PeakActiveCount.ToString());
                    DrawValue("Average", pool.AverageActiveCount.ToString("F1"));
                    DrawValue("Pooled", pool.PooledCount.ToString());
                }
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawValue("Initial", pool.InitialPoolSize.ToString());
                    DrawValue("Max", pool.MaxPoolSize.ToString());
                    DrawValue("Recommended", pool.RecommendedPoolSize.ToString());
                    DrawValue("Expandable", pool.IsExpandable ? "Yes" : "No");
                }
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawValue("Get", pool.GetCount.ToString());
                    DrawValue("Reuse", pool.ReuseCount.ToString());
                    DrawValue("Expanded", pool.ExpandedCreateCount.ToString());
                    DrawValue("Miss", pool.MissCount.ToString());
                }
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawValue("Reuse Rate", $"{pool.ReuseRate * 100f:F1}%");
                    DrawValue("Expansion Rate", $"{pool.ExpansionRate * 100f:F1}%");
                    DrawValue("Avg Life", $"{pool.AverageLifetime:F2}s");
                    DrawValue("Max Life", $"{pool.MaxLifetime:F2}s");
                }
            }
        }
        
        private void DrawValue(string label, string value)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(120f)))
            {
                GUILayout.Label(label, EditorStyles.miniLabel);
                GUILayout.Label(value, EditorStyles.boldLabel);
            }
        }
        
        private void DrawActiveObjects(JxObjectPool pool)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label($"Active Objects ({pool.ActiveCount})", EditorStyles.boldLabel);
                
                if (pool.ActiveCount == 0)
                {
                    GUILayout.Label("No active objects.");
                    return;
                }
                
                var activeObjects = pool.ActiveObjects
                    .Where(obj => obj != null)
                    .OrderByDescending(pool.GetActiveLifetime)
                    .ToList();
                
                foreach (var obj in activeObjects)
                {
                    DrawActiveObjectRow(pool, obj);
                }
            }
        }
        
        private void DrawActiveObjectRow(JxObjectPool pool, GameObject obj)
        {
            var lifetime = pool.GetActiveLifetime(obj);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(obj.name, GUILayout.Width(240f));
                GUILayout.Label($"{lifetime:F2}s", GUILayout.Width(80f));
                
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Select", GUILayout.Width(60f)))
                {
                    Selection.activeGameObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
            }
        }
        
        private void RefreshPoolStates(ObjectPoolManager manager)
        {
            foreach (var pair in manager.Pools)
            {
                var pool = pair.Value;
                
                if (pool == null)
                {
                    continue;
                }
                
                _visibleStates.TryAdd(pool, true);
                
                if (_selectedPool == null)
                {
                    _selectedPool = pool;
                }
            }
            
            var destroyedPools = _visibleStates.Keys
                .Where(pool => pool == null)
                .ToList();
            
            foreach (var pool in destroyedPools)
            {
                _visibleStates.Remove(pool);
                _poolColors.Remove(pool);
            }
            
            if (_selectedPool == null)
            {
                _selectedPool = manager.Pools.Values.FirstOrDefault(pool => pool != null);
            }
        }
        
        private bool GetVisibleState(JxObjectPool pool)
        {
            if (!_visibleStates.TryGetValue(pool, out var visible))
            {
                visible = true;
                _visibleStates.Add(pool, visible);
            }
            
            return visible;
        }
        
        private Color GetPoolColor(JxObjectPool pool)
        {
            if (_poolColors.TryGetValue(pool, out var color))
            {
                return color;
            }
            
            var hash = pool.PoolName.GetHashCode();
            var hue = Mathf.Abs(hash % 1000) / 1000f;
            
            color = Color.HSVToRGB(hue, 0.65f, 1f);
            
            _poolColors.Add(pool, color);
            
            return color;
        }
        
        private void SetAllVisible(bool visible)
        {
            var manager = FindFirstObjectByType<ObjectPoolManager>();
            
            if (manager == null)
            {
                return;
            }
            
            foreach (var pair in manager.Pools)
            {
                if (pair.Value == null)
                {
                    continue;
                }
                
                _visibleStates[pair.Value] = visible;
            }
        }
        
        private void ResetAll()
        {
            var manager = FindFirstObjectByType<ObjectPoolManager>();
            
            if (manager == null)
            {
                return;
            }
            
            foreach (var pair in manager.Pools)
            {
                pair.Value?.ResetProfiler();
            }
        }
        
        private string GetStatusText(JxObjectPool.EPoolStatus status)
        {
            return status switch
            {
                JxObjectPool.EPoolStatus.Oversized => "OVERSIZED",
                JxObjectPool.EPoolStatus.Healthy => "HEALTHY",
                JxObjectPool.EPoolStatus.Tight => "TIGHT",
                JxObjectPool.EPoolStatus.Undersized => "UNDERSIZED",
                _ => "-"
            };
        }
        
        private Color GetStatusColor(JxObjectPool.EPoolStatus status)
        {
            return status switch
            {
                JxObjectPool.EPoolStatus.Oversized => new Color(0.4f, 0.7f, 1f),
                JxObjectPool.EPoolStatus.Healthy => new Color(0.4f, 1f, 0.4f),
                JxObjectPool.EPoolStatus.Tight => new Color(1f, 0.8f, 0.25f),
                JxObjectPool.EPoolStatus.Undersized => new Color(1f, 0.3f, 0.3f),
                _ => Color.white
            };
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