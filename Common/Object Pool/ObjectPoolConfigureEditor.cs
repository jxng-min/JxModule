#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JxModule
{
    [CustomEditor(typeof(ObjectPoolConfigure))]
    public class ObjectPoolConfigureEditor : Editor
    {
        private SerializedProperty _configsProperty;
        private GUIStyle _titleStyle;
        private int _selectedIndex = -1;
        private readonly HashSet<GameObject> _duplicatePrefabs = new();
        
        private void OnEnable()
        {
            _configsProperty = serializedObject.FindProperty("configs");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            RefreshDuplicatePrefabs();
            
            EditorGUILayout.Space(8f);
            DrawSummary();
            
            EditorGUILayout.Space(6f);
            DrawTableHeader();
            DrawConfigList();
            
            EditorGUILayout.Space(10f);
            DrawButtons();
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawSummary()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Managed Pools", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(_configsProperty.arraySize.ToString(), EditorStyles.boldLabel, GUILayout.Width(40f));
            }
        }
        
        private void DrawTableHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Prefab", EditorStyles.boldLabel, GUILayout.MinWidth(160f));
                GUILayout.Label("Initial", EditorStyles.boldLabel, GUILayout.Width(55f));
                GUILayout.Label("Max", EditorStyles.boldLabel, GUILayout.Width(55f));
                GUILayout.Label("Expand", EditorStyles.boldLabel, GUILayout.Width(55f));
                GUILayout.Label("State", EditorStyles.boldLabel, GUILayout.Width(90f));
            }
        }
        
        private void DrawConfigList()
        {
            for (var i = 0; i < _configsProperty.arraySize; i++)
            {
                var element = _configsProperty.GetArrayElementAtIndex(i);
                var config = element.objectReferenceValue as JxObjectPoolConfig;
                
                if (config == null)
                {
                    DrawMissingConfig(i);
                    continue;
                }
                
                DrawConfigRow(i, config);
            }
        }
        
        private void DrawConfigRow(int index, JxObjectPoolConfig config)
        {
            var configSerializedObject = new SerializedObject(config);
            
            configSerializedObject.Update();
            
            var prefabProperty = configSerializedObject.FindProperty("prefab");
            var initialPoolSizeProperty = configSerializedObject.FindProperty("initialPoolSize");
            var maxPoolSizeProperty = configSerializedObject.FindProperty("maxPoolSize");
            var isExpandableProperty = configSerializedObject.FindProperty("isExpandable");
            
            var prefab = prefabProperty.objectReferenceValue as GameObject;
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawSelectionToggle(index);
                    
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(prefabProperty, GUIContent.none, GUILayout.MinWidth(160f));
                    EditorGUILayout.PropertyField(initialPoolSizeProperty, GUIContent.none, GUILayout.Width(55f));
                    EditorGUILayout.PropertyField(maxPoolSizeProperty, GUIContent.none, GUILayout.Width(55f));
                    EditorGUILayout.PropertyField(isExpandableProperty, GUIContent.none, GUILayout.Width(55f));
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        configSerializedObject.ApplyModifiedProperties();
                        
                        prefab = prefabProperty.objectReferenceValue as GameObject;
                        
                        RenameConfig(config, prefab);
                        EditorUtility.SetDirty(config);
                        RefreshDuplicatePrefabs();
                    }
                    
                    DrawState(prefab, initialPoolSizeProperty.intValue, maxPoolSizeProperty.intValue);
                }
                
                DrawValidationMessage(prefab, initialPoolSizeProperty.intValue, maxPoolSizeProperty.intValue);
            }
            
            configSerializedObject.ApplyModifiedProperties();
        }
        
        private void DrawSelectionToggle(int index)
        {
            var selected = _selectedIndex == index;
            
            var newSelected = GUILayout.Toggle(selected, GUIContent.none, GUILayout.Width(18f));
            
            if (newSelected)
            {
                _selectedIndex = index;
            }
            else if (selected)
            {
                _selectedIndex = -1;
            }
        }
        
        private void DrawState(GameObject prefab,
                               int initialPoolSize,
                               int maxPoolSize)
        {
            var state = GetState(prefab, initialPoolSize, maxPoolSize);
            var previousColor = GUI.contentColor;
            
            GUI.contentColor = GetStateColor(state);
            GUILayout.Label(state, EditorStyles.boldLabel, GUILayout.Width(90f));
            GUI.contentColor = previousColor;
        }
        
        private void DrawValidationMessage(GameObject prefab,
                                           int initialPoolSize,
                                           int maxPoolSize)
        {
            if (prefab == null)
            {
                EditorGUILayout.HelpBox("Prefab is not assigned.", MessageType.Warning);
                return;
            }
            
            if (_duplicatePrefabs.Contains(prefab))
            {
                EditorGUILayout.HelpBox($"Duplicate prefab detected: {prefab.name}", MessageType.Error);
                return;
            }
            
            if (initialPoolSize < 0)
            {
                EditorGUILayout.HelpBox("Initial Pool Size cannot be negative.", MessageType.Error);
                return;
            }
            
            if (maxPoolSize <= 0)
            {
                EditorGUILayout.HelpBox("Max Pool Size must be greater than 0.", MessageType.Error);
                return;
            }
            
            if (initialPoolSize > maxPoolSize)
            {
                EditorGUILayout.HelpBox("Initial Pool Size cannot be greater than Max Pool Size.", MessageType.Error);
            }
        }
        
        private string GetState(GameObject prefab,
                                int initialPoolSize,
                                int maxPoolSize)
        {
            if (prefab == null)
            {
                return "NO PREFAB";
            }
            
            if (_duplicatePrefabs.Contains(prefab))
            {
                return "DUPLICATE";
            }
            
            if (initialPoolSize < 0 ||
                maxPoolSize <= 0 ||
                initialPoolSize > maxPoolSize)
            {
                return "INVALID";
            }
            
            return "OK";
        }
        
        private Color GetStateColor(string state)
        {
            return state switch
            {
                "OK" => new Color(0.4f, 1f, 0.4f),
                "NO PREFAB" => new Color(1f, 0.8f, 0.3f),
                "DUPLICATE" => new Color(1f, 0.35f, 0.35f),
                "INVALID" => new Color(1f, 0.35f, 0.35f),
                _ => Color.white
            };
        }
        
        private void DrawMissingConfig(int index)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label($"Missing Config [{index}]", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    _configsProperty.DeleteArrayElementAtIndex(index);
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
        
        private void DrawButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ Add Config", GUILayout.Height(28f)))
                {
                    AddConfig();
                }
                
                GUI.enabled = _selectedIndex >= 0 &&
                              _selectedIndex < _configsProperty.arraySize;
                
                if (GUILayout.Button("Remove Selected", GUILayout.Height(28f)))
                {
                    RemoveSelectedConfig();
                }
                
                GUI.enabled = true;
            }
        }
        
        private void AddConfig()
        {
            var configure = (ObjectPoolConfigure)target;
            var config = CreateInstance<JxObjectPoolConfig>();
            config.name = $"Pool Config {_configsProperty.arraySize + 1}";
            
            Undo.RegisterCreatedObjectUndo(config, "Add Object Pool Config");
            AssetDatabase.AddObjectToAsset(config, configure);
            
            serializedObject.Update();
            
            var index = _configsProperty.arraySize;
            
            _configsProperty.InsertArrayElementAtIndex(index);
            _configsProperty.GetArrayElementAtIndex(index).objectReferenceValue = config;
            serializedObject.ApplyModifiedProperties();
            
            EditorUtility.SetDirty(configure);
            EditorUtility.SetDirty(config);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(configure));
            
            _selectedIndex = index;
            
            RefreshDuplicatePrefabs();
        }
        
        private void RemoveSelectedConfig()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _configsProperty.arraySize)
            {
                return;
            }
            
            var configure = (ObjectPoolConfigure)target;
            var element = _configsProperty.GetArrayElementAtIndex(_selectedIndex);
            var config = element.objectReferenceValue as JxObjectPoolConfig;
            element.objectReferenceValue = null;
            
            _configsProperty.DeleteArrayElementAtIndex(_selectedIndex);
            serializedObject.ApplyModifiedProperties();
            
            if (config != null)
            {
                Undo.DestroyObjectImmediate(config);
            }
            
            EditorUtility.SetDirty(configure);
            AssetDatabase.SaveAssets();
            
            _selectedIndex = -1;
            
            RefreshDuplicatePrefabs();
        }
        
        private void RenameConfig(JxObjectPoolConfig config, GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }
            
            var targetName = $"{prefab.name} Config";
            if (config.name == targetName)
            {
                return;
            }
            
            config.name = targetName;
            
            EditorUtility.SetDirty(config);
        }
        
        private void RefreshDuplicatePrefabs()
        {
            _duplicatePrefabs.Clear();
            
            var prefabCount = new Dictionary<GameObject, int>();
            
            for (var i = 0; i < _configsProperty.arraySize; i++)
            {
                var element = _configsProperty.GetArrayElementAtIndex(i);
                
                var config = element.objectReferenceValue as JxObjectPoolConfig;
                if (config == null || config.Prefab == null)
                {
                    continue;
                }
                
                if (!prefabCount.TryAdd(config.Prefab, 1))
                {
                    prefabCount[config.Prefab]++;
                }
            }
            
            foreach (var pair in prefabCount)
            {
                if (pair.Value > 1)
                {
                    _duplicatePrefabs.Add(pair.Key);
                }
            }
        }
    }
}

#endif