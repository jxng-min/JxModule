#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace JxModule.DataTable
{
    public class DataTableCreationWindow : EditorWindow
    {
        private static readonly Vector2 WindowSize = new(430f, 220f);
        private static readonly Color AccentColor = new(0.35f, 0.65f, 1f);

        private DataTableConfigure _configuration;
        private string _tableName = "New DataTable";
        private Type[] _availableRowTypes = Array.Empty<Type>();
        private string[] _availableRowTypeNames = Array.Empty<string>();
        private int _selectedRowTypeIndex;

        public static void Open(DataTableConfigure configuration)
        {
            var window = GetWindow<DataTableCreationWindow>(true, "Create DataTable", true);

            window._configuration = configuration;
            window.minSize = WindowSize;
            window.maxSize = WindowSize;
            window.RefreshRowTypes();
            window.Show();
        }

        private void OnEnable()
        {
            minSize = WindowSize;
            maxSize = WindowSize;
            RefreshRowTypes();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawForm();
            DrawFooter();
        }

        private void DrawHeader()
        {
            var rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), AccentColor);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Create DataTable", new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15
            });
            EditorGUILayout.LabelField("Creates a DataTable asset and matching CSV template.", EditorStyles.miniLabel);
            EditorGUILayout.Space(2f);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
        }

        private void DrawForm()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _tableName = EditorGUILayout.TextField("Name", _tableName);

            if (_availableRowTypes.Length > 0)
            {
                _selectedRowTypeIndex = EditorGUILayout.Popup("Row Type", _selectedRowTypeIndex, _availableRowTypeNames);
            }
            else
            {
                EditorGUILayout.HelpBox("No DataTableRowBase derived types found.", MessageType.Warning);
            }

            if (_configuration != null)
            {
                DrawReadonlyPath("DataTable Path", $"{_configuration.DataTableDirectory}/{_tableName.Trim()}.asset");
                DrawReadonlyPath("CSV Path", $"{_configuration.CsvDirectory}/{_tableName.Trim()}.csv");
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawReadonlyPath(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel, GUILayout.Width(88f));
            EditorGUILayout.SelectableLabel(value, EditorStyles.miniLabel, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", GUILayout.Width(110f), GUILayout.Height(30f)))
            {
                Close();
            }

            using (new EditorGUI.DisabledScope(!CanCreate()))
            {
                if (GUILayout.Button("Create", GUILayout.Width(110f), GUILayout.Height(30f)))
                {
                    CreateDataTable();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void RefreshRowTypes()
        {
            _availableRowTypes = TypeCache
                .GetTypesDerivedFrom<DataTableRowBase>()
                .Where(x => !x.IsAbstract && !x.IsGenericType)
                .OrderBy(x => x.Name)
                .ToArray();

            _availableRowTypeNames = _availableRowTypes
                .Select(x => x.Name)
                .ToArray();

            if (_selectedRowTypeIndex >= _availableRowTypes.Length)
            {
                _selectedRowTypeIndex = 0;
            }
        }

        private void CreateDataTable()
        {
            var tableName = _tableName.Trim();
            var tableDirectory = _configuration.DataTableDirectory;
            var csvDirectory = _configuration.CsvDirectory;

            EnsureDirectory(tableDirectory);
            EnsureDirectory(csvDirectory);

            var tablePath = $"{tableDirectory}/{tableName}.asset";
            var csvPath = $"{csvDirectory}/{tableName}.csv";

            if (AssetDatabase.LoadAssetAtPath<DataTable>(tablePath))
            {
                EditorUtility.DisplayDialog("Create DataTable", $"DataTable already exists.\n\n{tablePath}", "OK");
                return;
            }

            if (File.Exists(csvPath))
            {
                EditorUtility.DisplayDialog("Create DataTable", $"CSV already exists.\n\n{csvPath}", "OK");
                return;
            }

            var rowType = _availableRowTypes[_selectedRowTypeIndex];
            var dataTable = CreateInstance<DataTable>();
            dataTable.name = tableName;

            AssetDatabase.CreateAsset(dataTable, tablePath);
            CreateCsvTemplate(csvPath, rowType);
            AssetDatabase.Refresh();

            var csvAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(csvPath);
            SetupDataTable(dataTable, rowType, csvAsset);
            RegisterDataTable(dataTable);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(dataTable);
            Selection.activeObject = dataTable;
            Close();
        }

        private static void SetupDataTable(DataTable dataTable, Type rowType, TextAsset csvAsset)
        {
            var serializedTable = new SerializedObject(dataTable);
            serializedTable.Update();

            serializedTable.FindProperty(DataTable.RowTypePropertyName).stringValue = rowType.AssemblyQualifiedName;
            serializedTable.FindProperty(DataTable.CsvPropertyName).objectReferenceValue = csvAsset;

            serializedTable.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(dataTable);
        }

        private void RegisterDataTable(DataTable dataTable)
        {
            var serializedConfiguration = new SerializedObject(_configuration);
            serializedConfiguration.Update();

            var dataTablesProperty = serializedConfiguration.FindProperty(DataTableConfigure.DataTablesPropertyName);

            for (var i = 0; i < dataTablesProperty.arraySize; i++)
            {
                var element = dataTablesProperty.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == dataTable)
                {
                    return;
                }
            }

            dataTablesProperty.arraySize++;

            var newElement = dataTablesProperty.GetArrayElementAtIndex(dataTablesProperty.arraySize - 1);
            newElement.objectReferenceValue = dataTable;

            serializedConfiguration.ApplyModifiedProperties();
        }

        private static void CreateCsvTemplate(string path, Type rowType)
        {
            var columns = new List<string>
            {
                "rowID",
                "isEnable"
            };

            foreach (var field in GetRowFields(rowType))
            {
                if (field.Name is "rowID" or "isEnable")
                {
                    continue;
                }

                columns.Add(field.Name);
            }

            File.WriteAllText(path, string.Join(",", columns));
        }

        private static List<FieldInfo> GetRowFields(Type rowType)
        {
            var fields = new List<FieldInfo>();
            var hierarchy = new Stack<Type>();
            var currentType = rowType;

            while (currentType != null && currentType != typeof(DataTableRowBase))
            {
                hierarchy.Push(currentType);
                currentType = currentType.BaseType;
            }

            while (hierarchy.Count > 0)
            {
                var type = hierarchy.Pop();
                fields.AddRange(type
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(IsSerializedField)
                    .OrderBy(x => x.MetadataToken));
            }

            return fields;
        }

        private static bool IsSerializedField(FieldInfo field)
        {
            if (field.IsStatic || field.IsNotSerialized)
            {
                return false;
            }

            return field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;
        }

        private bool CanCreate()
        {
            return _configuration != null &&
                   !string.IsNullOrWhiteSpace(_tableName) &&
                   _availableRowTypes.Length > 0 &&
                   IsValidAssetDirectory(_configuration.DataTableDirectory) &&
                   IsValidAssetDirectory(_configuration.CsvDirectory);
        }

        private static bool IsValidAssetDirectory(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                   (path == "Assets" || path.StartsWith("Assets/"));
        }

        private static void EnsureDirectory(string path)
        {
            var folders = path.Split('/');
            var currentPath = folders[0];

            for (var i = 1; i < folders.Length; i++)
            {
                var nextPath = $"{currentPath}/{folders[i]}";

                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }

                currentPath = nextPath;
            }
        }
    }
}
#endif
