#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JxModule
{
    public class EffectManagement : EditorWindow
    {
        private readonly List<ManagedEffectEntry> _sceneEntries = new();
        private readonly Dictionary<string, bool> _categoryFoldouts = new();

        private readonly List<ManagedEffectEntry> _prefabEntries = new();
        private readonly Dictionary<string, bool> _prefabFoldouts = new();
        
        private string _searchText = string.Empty;
        private Vector2 _scrollPosition;

        private bool _sceneSectionExpanded = true;
        private bool _prefabSectionExpanded = true;

        [MenuItem("JxModule/Effect Management")]
        private static void Open()
        {
            var window = GetWindow<EffectManagement>();
            
            window.titleContent = new GUIContent("Effect Management");
            window.minSize = new Vector2(450f, 300f);

            window.Show();
        }

        private void OnEnable() => Refresh();
        private void OnDisable() => ClearEntries();

        private void OnGUI()
        {
            DrawToolbar();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawSceneSection();
            EditorGUILayout.Space(8f);
            DrawPrefabSection();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _searchText = GUILayout.TextField(_searchText, 
                                                  GUI.skin.FindStyle("ToolbarSearchTextField"),
                                                  GUILayout.MinWidth(150f));

                if (!string.IsNullOrEmpty(_searchText))
                {
                    if (GUILayout.Button(GUIContent.none, GUI.skin.FindStyle("ToolbarSearchCancelButton")))
                    {
                        _searchText = string.Empty;
                        GUI.FocusControl(null);
                    }
                }

                GUILayout.FlexibleSpace();
                
                GUILayout.Label($"Effects: {_sceneEntries.Count}", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                {
                    Refresh();
                }
            }
        }

        private void DrawSceneSection()
        {
            _sceneSectionExpanded = EditorGUILayout.Foldout(_sceneSectionExpanded, "Scene Effects", true, EditorStyles.foldoutHeader);
            if (!_sceneSectionExpanded)
            {
                return;
            }
            
            var filteredEntries = _sceneEntries
                .Where(IsSearchMatch)
                .ToList();

            if (filteredEntries.Count == 0)
            {
                EditorGUILayout.HelpBox(string.IsNullOrWhiteSpace(_searchText) ? "No component with EffectDebugAttribute was found in the current scene."
                                                                               : "No search results found.", MessageType.Info);
                return;  
            }
            
            var categoryGroups = filteredEntries
                .GroupBy(entry => entry.attribute.Category)
                .OrderBy(group => group.Key);
            
            foreach (var group in categoryGroups)
            {
                DrawSceneCategory(group.Key, group.ToList());
                EditorGUILayout.Space(4f);
            }
        }

        private void DrawPrefabSection()
        {
            _prefabSectionExpanded = EditorGUILayout.Foldout(_prefabSectionExpanded, "Prefab Effects", true, EditorStyles.foldoutHeader);
            if (!_prefabSectionExpanded)
            {
                return;
            }
            
            var filteredEntries = _prefabEntries
                .Where(IsSearchMatch)
                .ToList();
            
            if (filteredEntries.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    string.IsNullOrWhiteSpace(_searchText) ? "No Prefab with EffectDebugAttribute was found."
                                                           : "No search results found.", MessageType.Info);
                return;
            }
            
            var categoryGroups = filteredEntries
                .GroupBy(entry => entry.attribute.Category)
                .OrderBy(group => group.Key);

            foreach (var categoryGroup in categoryGroups)
            {
                DrawPrefabCategory(categoryGroup.Key, categoryGroup.ToList());
                EditorGUILayout.Space(4f);
            }
        }

        private void DrawSceneCategory(string category, IReadOnlyList<ManagedEffectEntry> entries)
        {
            var foldoutKey = $"Scene/{category}";
            if (!_categoryFoldouts.TryGetValue(foldoutKey, out var isExpanded))
            {
                isExpanded = true;
                _categoryFoldouts.Add(foldoutKey, true);
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                isExpanded = EditorGUILayout.Foldout(isExpanded,
                                                     $"{category} ({GetDistinctEffectCount(entries)})",
                                                     true,
                                                     EditorStyles.foldoutHeader);
                
                _categoryFoldouts[foldoutKey] = isExpanded;
                if (!isExpanded)
                {
                    return;
                }
                
                EditorGUILayout.Space(2f);

                var groupedEntries = entries
                    .Where(entry => entry.target != null)
                    .GroupBy(entry => entry.target.GetType())
                    .OrderBy(group => group.First().attribute.Order)
                    .ThenBy(group => group.First().attribute.Name);

                foreach (var group in groupedEntries)
                {
                    DrawSceneEntry(group.ToList());
                }
            }
        }
        
        private void DrawSceneEntry(IReadOnlyList<ManagedEffectEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            var representative = entries[0];
            if (representative.target == null)
            {
                return;
            }

            var label = entries.Count > 1 ? $"{representative.attribute.Name} ({entries.Count})"
                                                 : representative.attribute.Name;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                representative.isExpanded = EditorGUILayout.Foldout(representative.isExpanded, label, true);
                if (!representative.isExpanded)
                {
                    return;
                }

                EditorGUILayout.Space(4f);
                DrawSceneTargetInfo(entries);
                EditorGUILayout.Space(6f);
                DrawInspector(entries);
            }
        }

        private void DrawSceneTargetInfo(IReadOnlyList<ManagedEffectEntry> entries)
        {
            if (entries.Count == 1)
            {
                DrawSingleSceneTargetInfo(entries[0]);
                return;
            }
            
            EditorGUILayout.LabelField($"Attached Objects ({entries.Count})", EditorStyles.boldLabel);
            
            foreach (var entry in entries)
            {
                if (entry.target == null)
                {
                    continue;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(entry.target.gameObject, typeof(GameObject), true);

                    if (GUILayout.Button("Select", GUILayout.Width(60f)))
                    {
                        Selection.activeGameObject = entry.target.gameObject;
                        EditorGUIUtility.PingObject(entry.target.gameObject);
                    }
                }
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select All"))
                {
                    Selection.objects = entries
                        .Where(x => x.target != null)
                        .Select(x => (UnityEngine.Object)x.target.gameObject)
                        .ToArray();
                }

                if (GUILayout.Button("Ping Script"))
                {
                    var script = MonoScript.FromMonoBehaviour(entries[0].target);

                    if (script != null)
                    {
                        Selection.activeObject = script;
                        EditorGUIUtility.PingObject(script);
                    }
                }
            }
        }
        
        private void DrawSingleSceneTargetInfo(ManagedEffectEntry entry)
        {
            var hierarchyPath = GetHierarchyPath(entry.target.transform);
            EditorGUILayout.LabelField("Hierarchy Path", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(hierarchyPath,
                                            EditorStyles.textField,
                                            GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.Space(4f);
            
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Attached Object", entry.target.gameObject, typeof(GameObject), true);
                EditorGUILayout.ObjectField("Component", entry.target, entry.target.GetType(), true);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select Object"))
                {
                    Selection.activeGameObject = entry.target.gameObject;
                    EditorGUIUtility.PingObject(entry.target.gameObject);
                }

                if (GUILayout.Button("Ping Script"))
                {
                    var script = MonoScript.FromMonoBehaviour(entry.target);

                    if (script != null)
                    {
                        Selection.activeObject = script;
                        EditorGUIUtility.PingObject(script);
                    }
                }
            }
        }

        private void DrawPrefabCategory(string category, IReadOnlyList<ManagedEffectEntry> entries)
        {
            var foldoutKey = $"Prefab/{category}";

            if (!_categoryFoldouts.TryGetValue(foldoutKey, out var isExpanded))
            {
                isExpanded = true;
                _categoryFoldouts.Add(foldoutKey, true);
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var prefabCount = entries
                    .Select(x => x.assetPath)
                    .Distinct()
                    .Count();
                
                isExpanded = EditorGUILayout.Foldout(isExpanded,
                                                     $"{category} ({prefabCount})",
                                                     true,
                                                     EditorStyles.foldoutHeader);
                
                
                _categoryFoldouts[foldoutKey] = isExpanded;
                if (!isExpanded)
                {
                    return;
                }
                
                var prefabGroups = entries
                    .Where(entry => entry.target != null && !string.IsNullOrEmpty(entry.assetPath))
                    .GroupBy(entry => entry.assetPath)
                    .OrderBy(group => group.Key);

                foreach (var prefabGroup in prefabGroups)
                {
                    DrawPrefabAsset(prefabGroup.Key, prefabGroup.ToList());
                }
            }
        }
        
        private void DrawPrefabAsset(string assetPath, IReadOnlyList<ManagedEffectEntry> entries)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            var prefabName = prefab != null ? prefab.name : assetPath;

            if (!_prefabFoldouts.TryGetValue(assetPath, out var isExpanded))
            {
                isExpanded = false;
                _prefabFoldouts.Add(assetPath, false);
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                isExpanded = EditorGUILayout.Foldout(isExpanded, $"{prefabName} ({GetDistinctEffectCount(entries)})", true);
                _prefabFoldouts[assetPath] = isExpanded;

                if (!isExpanded)
                {
                    return;
                }

                EditorGUILayout.Space(2f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);
                    }

                    if (GUILayout.Button("Open", GUILayout.Width(60f)))
                    {
                        PrefabStageUtility.OpenPrefab(assetPath);
                    }
                }

                EditorGUILayout.LabelField("Asset Path", assetPath);
                EditorGUILayout.Space(4f);

                var groupedEntries = entries
                    .GroupBy(entry => entry.target.GetType())
                    .OrderBy(group => group.First().attribute.Order)
                    .ThenBy(group => group.First().attribute.Name);

                foreach (var group in groupedEntries)
                {
                    DrawPrefabEntry(group.ToList());
                }
            }
        }
        
        private void DrawPrefabEntry(IReadOnlyList<ManagedEffectEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            var representative = entries[0];
            if (representative.target == null)
            {
                return;
            }

            var label = entries.Count > 1 ? $"{representative.attribute.Name} ({entries.Count})"
                                                 : representative.attribute.Name;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                representative.isExpanded = EditorGUILayout.Foldout(representative.isExpanded, label, true);
                if (!representative.isExpanded)
                {
                    return;
                }

                EditorGUILayout.Space(4f);
                DrawPrefabTargetInfo(entries);
                EditorGUILayout.Space(6f);
                DrawInspector(entries);
            }
        }
        
        
        private void DrawPrefabTargetInfo(
            IReadOnlyList<ManagedEffectEntry> entries)
        {
            var representative = entries[0];
            var prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(representative.assetPath);

            EditorGUILayout.LabelField(entries.Count > 1 ? $"Prefab Objects ({entries.Count})" : "Prefab Object", EditorStyles.boldLabel);

            foreach (var entry in entries)
            {
                if (entry.target == null)
                {
                    continue;
                }

                var localPath = GetRelativePath(prefabRoot != null ? prefabRoot.transform : null, entry.target.transform);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(localPath);

                    if (GUILayout.Button("Ping", GUILayout.Width(60f)))
                    {
                        EditorGUIUtility.PingObject(entry.target);
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Prefab"))
                {
                    PrefabStageUtility.OpenPrefab(representative.assetPath);
                }

                if (GUILayout.Button("Ping Script"))
                {
                    var script = MonoScript.FromMonoBehaviour(representative.target);

                    if (script != null)
                    {
                        Selection.activeObject = script;
                        EditorGUIUtility.PingObject(script);
                    }
                }
            }
        }
        
        private void DrawInspector(IReadOnlyList<ManagedEffectEntry> entries)
        {
            var validEntries = entries
                .Where(x => x.target != null)
                .ToList();
            
            if (validEntries.Count == 0)
            {
                return;
            }
            
            var representative = validEntries[0];

            var targets = validEntries
                .Select(x => (UnityEngine.Object)x.target)
                .ToArray();

            var needRecreate = 
                representative.inspector == null || 
                representative.inspector.targets.Length != targets.Length || 
                !representative.inspector.targets.SequenceEqual(targets);
            
            if (needRecreate)
            {
                if (representative.inspector != null)
                {
                    DestroyImmediate(representative.inspector);
                }

                representative.inspector = Editor.CreateEditor(targets);
            }
            
            if (representative.inspector == null)
            {
                EditorGUILayout.HelpBox("Failed to create target Inspector.", MessageType.Warning);
                return;
            }
            
            EditorGUILayout.LabelField(validEntries.Count > 1 ? $"Effect Settings - Multi Edit ({validEntries.Count})" 
                                                              : "Effect Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(2f);

            representative.inspector.OnInspectorGUI();
        }
        
        private bool IsSearchMatch(ManagedEffectEntry entry)
        {
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                return true;
            }
            
            if (entry.target == null)
            {
                return false;
            }

            var search = _searchText.Trim();
            return ContainsIgnoreCase(entry.attribute.Name, search) ||
                   ContainsIgnoreCase(entry.target.GetType().Name, search) ||
                   ContainsIgnoreCase(entry.target.gameObject.name, search) ||
                   ContainsIgnoreCase(entry.attribute.Category, search) ||
                   ContainsIgnoreCase(GetHierarchyPath(entry.target.transform), search) ||
                   ContainsIgnoreCase(entry.assetPath, search);
        }
        
        private static bool ContainsIgnoreCase(string source, string search)
        {
            return !string.IsNullOrEmpty(source) && source.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        
        private void Refresh()
        {
            ClearEntries();

            CollectSceneEntries();
            CollectPrefabEntries();

            Repaint();
        }
        
        private void CollectSceneEntries()
        {
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }

                var type = behaviour.GetType();
                var attribute = type.GetCustomAttribute<ManagedEffectAttribute>();
                if (attribute == null)
                {
                    continue;
                }

                _sceneEntries.Add(
                    new ManagedEffectEntry
                    {
                        target = behaviour,
                        attribute = attribute,
                        inspector = null,
                        isExpanded = false
                    });

                _categoryFoldouts.TryAdd($"Scene/{attribute.Category}", true);
            }
        }

        private void CollectPrefabEntries()
        {
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");

            foreach (var guid in prefabGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefabRoot == null)
                {
                    continue;
                }

                var behaviours = prefabRoot.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var behaviour in behaviours)
                {
                    if (behaviour == null)
                    {
                        continue;
                    }

                    var type = behaviour.GetType();
                    var attribute = type.GetCustomAttribute<ManagedEffectAttribute>();
                    if (attribute == null)
                    {
                        continue;
                    }

                    _prefabEntries.Add(
                        new ManagedEffectEntry
                        {
                            target = behaviour,
                            attribute = attribute,
                            inspector = null,
                            isExpanded = false,
                            assetPath = assetPath
                        });

                    _categoryFoldouts.TryAdd($"Prefab/{attribute.Category}", true);
                }
            }
        }
        
        private void ClearEntries()
        {
            DestroyInspectors(_sceneEntries);
            DestroyInspectors(_prefabEntries);

            _sceneEntries.Clear();
            _prefabEntries.Clear();
        }
        
        private static void DestroyInspectors(IEnumerable<ManagedEffectEntry> entries)
        {
            foreach (var entry in entries)
            {
                if (entry.inspector != null)
                {
                    DestroyImmediate(entry.inspector);
                }
            }
        }
        
        private static int GetDistinctEffectCount(IReadOnlyList<ManagedEffectEntry> entries)
        {
            return entries
                .Where(x => x.target != null)
                .Select(x => x.target.GetType())
                .Distinct()
                .Count();
        }

        private static string GetHierarchyPath(Transform target)
        {
            if (target == null)
            {
                return string.Empty;
            }

            var path = target.name;
            while (target.parent != null)
            {
                target = target.parent;
                path = $"{target.name}/{path}";
            }

            return path;
        }
        
        private static string GetRelativePath(Transform root, Transform target)
        {
            if (target == null)
            {
                return string.Empty;
            }

            if (root == null)
            {
                return GetHierarchyPath(target);
            }

            if (target == root)
            {
                return root.name;
            }

            var names = new Stack<string>();

            var current = target;
            while (current != null && current != root)
            {
                names.Push(current.name);
                current = current.parent;
            }

            if (current == root)
            {
                names.Push(root.name);
            }

            return string.Join("/", names);
        }
    }
}
#endif