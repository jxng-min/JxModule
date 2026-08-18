#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace JxModule.DataTable
{
    [CustomEditor(typeof(DataTableConfigure))]
    public class DataTableConfigurationEditor : Editor
    {
        private static readonly Color AccentColor = new(0.35f, 0.65f, 1f);
        private static readonly Color OkColor = new(0.1f, 0.55f, 0.25f);
        private static readonly Color WarningColor = new(0.7f, 0.55f, 0.1f);
        private static readonly Color ErrorColor = new(0.7f, 0.18f, 0.18f);

        private readonly Dictionary<DataTable, bool> _tableFoldoutStates = new();

        private DataTableConfigure _configuration;
        private DataTable _selectedTable;

        private void OnEnable()
        {
            _configuration = target as DataTableConfigure;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (RemoveMissingTableReferences())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }

            DrawHeaderSection();
            DrawDirectorySection();
            DrawRegisteredTablesSection();
            DrawToolSection();

            serializedObject.ApplyModifiedProperties();
        }

        private bool RemoveMissingTableReferences()
        {
            var dataTablesProperty = serializedObject.FindProperty(DataTableConfigure.DataTablesPropertyName);
            var removedAny = false;

            for (var i = dataTablesProperty.arraySize - 1; i >= 0; i--)
            {
                var element = dataTablesProperty.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue)
                {
                    continue;
                }

                dataTablesProperty.DeleteArrayElementAtIndex(i);
                removedAny = true;
            }

            if (!removedAny)
            {
                return false;
            }

            _selectedTable = null;
            _tableFoldoutStates.Clear();
            return true;
        }

        private void DrawHeaderSection()
        {
            var rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), AccentColor);

            EditorGUILayout.Space(5f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("DataTable Configuration", new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15
            });
            GUILayout.FlexibleSpace();
            DrawBadge($"{_configuration.DataTables.Count} Tables", new Color(0.25f, 0.45f, 0.7f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Create, register, update, and sync DataTables from one place.", EditorStyles.miniLabel);
            EditorGUILayout.Space(2f);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
        }

        private void DrawDirectorySection()
        {
            var tableDirectoryProperty = serializedObject.FindProperty(DataTableConfigure.DataTableDirectoryPropertyName);
            var csvDirectoryProperty = serializedObject.FindProperty(DataTableConfigure.CsvDirectoryPropertyName);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Directories", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(tableDirectoryProperty, new GUIContent("DataTable Assets"));
            EditorGUILayout.PropertyField(csvDirectoryProperty, new GUIContent("CSV Files"));

            if (!IsValidAssetDirectory(tableDirectoryProperty.stringValue))
            {
                EditorGUILayout.HelpBox("DataTable directory must be inside Assets.", MessageType.Error);
            }

            if (!IsValidAssetDirectory(csvDirectoryProperty.stringValue))
            {
                EditorGUILayout.HelpBox("CSV directory must be inside Assets.", MessageType.Error);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
        }

        private void DrawRegisteredTablesSection()
        {
            var dataTables = _configuration.DataTables;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Registered Tables", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Click a row to select it.", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            if (dataTables.Count == 0)
            {
                EditorGUILayout.HelpBox("There are no registered DataTables.", MessageType.Info);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(6f);
                return;
            }

            foreach (var dataTable in dataTables)
            {
                DrawRegisteredTable(dataTable);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
        }

        private void DrawRegisteredTable(DataTable dataTable)
        {
            if (!dataTable)
            {
                EditorGUILayout.HelpBox("Missing DataTable", MessageType.Error);
                return;
            }

            _tableFoldoutStates.TryAdd(dataTable, false);

            var rowRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (_selectedTable == dataTable && Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.y, 3f, rowRect.height), AccentColor);
            }

            var headerRect = EditorGUILayout.BeginHorizontal();
            _tableFoldoutStates[dataTable] = GUILayout.Toggle(_tableFoldoutStates[dataTable], GUIContent.none, EditorStyles.foldout, GUILayout.Width(16f));
            EditorGUILayout.LabelField(dataTable.name, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            DrawTableState(dataTable);
            EditorGUILayout.EndHorizontal();

            if (_tableFoldoutStates[dataTable])
            {
                DrawTableDetails(dataTable);
            }

            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 &&
                headerRect.Contains(Event.current.mousePosition))
            {
                _selectedTable = dataTable;
                Repaint();
            }

            EditorGUILayout.Space(2f);
        }

        private void DrawTableDetails(DataTable dataTable)
        {
            var csvAsset = FindCsvAsset(dataTable);

            EditorGUILayout.Space(4f);
            DrawInfoRow("Row Type", GetRowTypeName(dataTable));
            DrawInfoRow("CSV", csvAsset ? csvAsset.name : "Missing");
            DrawInfoRow("Rows", CountRows(dataTable).ToString());
            EditorGUILayout.Space(4f);

            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(!CanUpdate(dataTable)))
            {
                if (GUILayout.Button("Update", GUILayout.Height(24f)))
                {
                    UpdateTable(dataTable);
                }
            }

            using (new EditorGUI.DisabledScope(!CanSync(dataTable)))
            {
                if (GUILayout.Button("Sync", GUILayout.Height(24f)))
                {
                    SyncTable(dataTable);
                }
            }

            if (GUILayout.Button("Ping", GUILayout.Height(24f)))
            {
                EditorGUIUtility.PingObject(dataTable);
            }

            using (new EditorGUI.DisabledScope(!csvAsset))
            {
                if (GUILayout.Button("Open CSV", GUILayout.Height(24f)))
                {
                    AssetDatabase.OpenAsset(csvAsset);
                }

                if (GUILayout.Button("Ping CSV", GUILayout.Height(24f)))
                {
                    EditorGUIUtility.PingObject(csvAsset);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTableState(DataTable dataTable)
        {
            var state = GetTableState(dataTable);
            var color = state switch
            {
                "OK" => OkColor,
                "No CSV" => WarningColor,
                _ => ErrorColor
            };

            DrawBadge(state, color, 76f);
        }

        private void DrawToolSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("+ Create DataTable", GUILayout.Height(28f)))
            {
                DataTableCreationWindow.Open(_configuration);
            }

            if (GUILayout.Button("Register Existing", GUILayout.Height(28f)))
            {
                ShowRegisterMenu();
            }

            using (new EditorGUI.DisabledScope(!_selectedTable))
            {
                if (GUILayout.Button("Remove Selected", GUILayout.Height(28f)))
                {
                    ShowRemoveMenu();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Update All", GUILayout.Height(32f)))
            {
                UpdateAllTables();
            }

            if (GUILayout.Button("Sync All", GUILayout.Height(32f)))
            {
                SyncAllTables();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private static void DrawBadge(string text, Color color, float width = 82f)
        {
            var rect = GUILayoutUtility.GetRect(width, 20f, GUILayout.Width(width), GUILayout.Height(20f));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, color);
            }

            GUI.Label(rect, text, new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            });
        }

        private static void DrawInfoRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel, GUILayout.Width(70f));
            EditorGUILayout.SelectableLabel(value, EditorStyles.label, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.EndHorizontal();
        }

        private void ShowRegisterMenu()
        {
            var menu = new GenericMenu();
            var dataTables = FindAllDataTables();

            if (dataTables.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No DataTables Found"));
            }

            foreach (var dataTable in dataTables)
            {
                var table = dataTable;
                if (_configuration.DataTables.Contains(table))
                {
                    menu.AddDisabledItem(new GUIContent(table.name));
                    continue;
                }

                menu.AddItem(new GUIContent(table.name), false, () => RegisterTable(table));
            }

            menu.ShowAsContext();
        }

        private void ShowRemoveMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Remove From Configuration"), false, RemoveSelectedTable);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete DataTable"), false, DeleteSelectedTable);
            menu.AddItem(new GUIContent("Delete DataTable + CSV"), false, DeleteSelectedTableWithCsv);
            menu.ShowAsContext();
        }

        private static List<DataTable> FindAllDataTables()
        {
            var guids = AssetDatabase.FindAssets("t:DataTable");

            return guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<DataTable>)
                .Where(x => x)
                .ToList();
        }

        private void RegisterTable(DataTable dataTable)
        {
            serializedObject.Update();

            var dataTablesProperty = serializedObject.FindProperty(DataTableConfigure.DataTablesPropertyName);
            dataTablesProperty.arraySize++;

            var element = dataTablesProperty.GetArrayElementAtIndex(dataTablesProperty.arraySize - 1);
            element.objectReferenceValue = dataTable;

            serializedObject.ApplyModifiedProperties();
        }

        private void RemoveSelectedTable()
        {
            if (!_selectedTable)
            {
                return;
            }

            serializedObject.Update();

            var dataTablesProperty = serializedObject.FindProperty(DataTableConfigure.DataTablesPropertyName);

            for (var i = 0; i < dataTablesProperty.arraySize; i++)
            {
                var element = dataTablesProperty.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue != _selectedTable)
                {
                    continue;
                }

                element.objectReferenceValue = null;
                dataTablesProperty.DeleteArrayElementAtIndex(i);
                break;
            }

            serializedObject.ApplyModifiedProperties();

            _tableFoldoutStates.Remove(_selectedTable);
            _selectedTable = null;
        }

        private void DeleteSelectedTable()
        {
            if (!_selectedTable)
            {
                return;
            }

            var dataTable = _selectedTable;
            var dataTablePath = AssetDatabase.GetAssetPath(dataTable);

            if (!IsPathInsideDirectory(dataTablePath, _configuration.DataTableDirectory))
            {
                EditorUtility.DisplayDialog("Delete DataTable", $"DataTable is outside the configured DataTable directory.\n\n{dataTablePath}", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Delete DataTable", $"Delete '{dataTable.name}'?\n\nCSV will not be deleted.", "Delete", "Cancel"))
            {
                return;
            }

            RemoveSelectedTable();
            AssetDatabase.DeleteAsset(dataTablePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void DeleteSelectedTableWithCsv()
        {
            if (!_selectedTable)
            {
                return;
            }

            var dataTable = _selectedTable;
            var dataTablePath = AssetDatabase.GetAssetPath(dataTable);
            var csvAsset = FindCsvAsset(dataTable);

            if (!IsPathInsideDirectory(dataTablePath, _configuration.DataTableDirectory))
            {
                EditorUtility.DisplayDialog("Delete DataTable", $"DataTable is outside the configured DataTable directory.\n\n{dataTablePath}", "OK");
                return;
            }

            if (!csvAsset)
            {
                EditorUtility.DisplayDialog("Delete DataTable", "CSV reference could not be found.", "OK");
                return;
            }

            var csvPath = AssetDatabase.GetAssetPath(csvAsset);
            if (!IsPathInsideDirectory(csvPath, _configuration.CsvDirectory))
            {
                EditorUtility.DisplayDialog("Delete DataTable", $"CSV is outside the configured CSV directory.\n\n{csvPath}", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Delete DataTable",
                    $"Delete '{dataTable.name}' and its CSV?\n\nDataTable:\n{dataTablePath}\n\nCSV:\n{csvPath}",
                    "Delete All",
                    "Cancel"))
            {
                return;
            }

            RemoveSelectedTable();
            AssetDatabase.DeleteAsset(dataTablePath);
            AssetDatabase.DeleteAsset(csvPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private async void SyncTable(DataTable dataTable)
        {
            if (!CanSync(dataTable))
            {
                return;
            }

            try
            {
                await RemoteDataTableSync.SyncAndRefresh(dataTable);
                Repaint();
            }
            catch (Exception e)
            {
                Debug.LogError($"DataTable Sync failed: {dataTable.name}\n{e}");
            }
        }

        private static bool IsPathInsideDirectory(string assetPath, string directory)
        {
            if (string.IsNullOrWhiteSpace(assetPath) || string.IsNullOrWhiteSpace(directory))
            {
                return false;
            }

            assetPath = assetPath.Replace("\\", "/").TrimEnd('/');
            directory = directory.Replace("\\", "/").TrimEnd('/');

            return assetPath.StartsWith(directory + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetRowTypeName(DataTable dataTable)
        {
            if (string.IsNullOrEmpty(dataTable.dataTableRowName))
            {
                return "-";
            }

            var type = Type.GetType(dataTable.dataTableRowName);
            return type?.Name ?? "Missing Type";
        }

        private static int CountRows(DataTable dataTable)
        {
            var path = AssetDatabase.GetAssetPath(dataTable);

            if (string.IsNullOrEmpty(path))
            {
                return 0;
            }

            return AssetDatabase
                .LoadAllAssetsAtPath(path)
                .Count(x => x is DataTableRowBase);
        }

        private static TextAsset FindCsvAsset(DataTable dataTable)
        {
            var serializedTable = new SerializedObject(dataTable);
            var property = serializedTable.GetIterator();

            if (!property.NextVisible(true))
            {
                return null;
            }

            do
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                if (property.objectReferenceValue is not TextAsset textAsset)
                {
                    continue;
                }

                var path = AssetDatabase.GetAssetPath(textAsset);
                if (!string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return textAsset;
            }
            while (property.NextVisible(false));

            return null;
        }

        private string GetTableState(DataTable dataTable)
        {
            if (!dataTable)
            {
                return "Missing";
            }

            if (_configuration.DataTables.Count(x => x == dataTable) > 1)
            {
                return "Duplicate";
            }

            if (string.IsNullOrEmpty(dataTable.dataTableRowName))
            {
                return "Invalid";
            }

            if (Type.GetType(dataTable.dataTableRowName) == null)
            {
                return "Invalid";
            }

            if (!FindCsvAsset(dataTable))
            {
                return "No CSV";
            }

            return "OK";
        }

        private static bool CanUpdate(DataTable dataTable)
        {
            return dataTable && dataTable.dataTableCsv && dataTable.GetRowType() != null;
        }

        private async void UpdateTable(DataTable dataTable)
        {
            if (!CanUpdate(dataTable))
            {
                return;
            }

            try
            {
                await dataTable.UpdateData(dataTable.dataTableCsv, null);
                Repaint();
            }
            catch (Exception e)
            {
                Debug.LogError($"DataTable Update failed: {dataTable.name}\n{e}");
            }
        }

        private async void UpdateAllTables()
        {
            var dataTables = _configuration.DataTables.Where(CanUpdate).ToList();

            if (dataTables.Count == 0)
            {
                EditorUtility.DisplayDialog("Update All", "There are no DataTables available to update.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Update All", $"Update {dataTables.Count} DataTables?", "Update", "Cancel"))
            {
                return;
            }

            try
            {
                for (var i = 0; i < dataTables.Count; i++)
                {
                    var dataTable = dataTables[i];
                    EditorUtility.DisplayProgressBar("Update DataTables", $"Updating {dataTable.name}...", (float)(i + 1) / dataTables.Count);
                    await dataTable.UpdateData(dataTable.dataTableCsv, null);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"DataTable Update All failed.\n{e}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Repaint();
        }

        private async void SyncAllTables()
        {
            var dataTables = _configuration.DataTables.Where(CanSync).ToList();

            if (dataTables.Count == 0)
            {
                EditorUtility.DisplayDialog("Sync All", "There are no DataTables available to sync.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Sync All", $"Sync {dataTables.Count} DataTables?", "Sync", "Cancel"))
            {
                return;
            }

            try
            {
                for (var i = 0; i < dataTables.Count; i++)
                {
                    var dataTable = dataTables[i];
                    EditorUtility.DisplayProgressBar("Sync DataTables", $"Syncing {dataTable.name}...", (float)(i + 1) / dataTables.Count);
                    await RemoteDataTableSync.SyncAndRefresh(dataTable);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"DataTable Sync All failed.\n{e}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Repaint();
        }

        private static bool CanSync(DataTable dataTable)
        {
            return dataTable && !string.IsNullOrWhiteSpace(dataTable.remoteCsvUrl);
        }

        private static bool IsValidAssetDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            return path == "Assets" || path.StartsWith("Assets/");
        }
    }
}
#endif
