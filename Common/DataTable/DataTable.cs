using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JxModule.DataTable
{
#region Editor
#if UNITY_EDITOR
    [CustomEditor(typeof(DataTable), true)]
    public class DataTableEditor : Editor
    {
        private SerializedProperty _remoteCsvUrlProperty;
        private bool _showFields = true;
        private bool _showRemoteSync;

        private void OnEnable()
        {
            _remoteCsvUrlProperty = serializedObject.FindProperty(DataTable.RemoteCsvUrlPropertyName);
        }

        public override void OnInspectorGUI()
        {
            var dataTable = target as DataTable;
            if (dataTable == null)
            {
                return;
            }

            var requestUpdate = false;
            var requestRemoteSync = false;

            serializedObject.Update();

            DrawHeader(dataTable);

            if (dataTable.dataTableCsv)
            {
                SyncCsv(dataTable);
                DrawSummary(dataTable);
                
                EditorGUILayout.Space(8f);
                _showFields = EditorGUILayout.Foldout(_showFields, "Row Fields", true);
                if (_showFields)
                {
                    DrawTableFields(dataTable);
                }
                
                EditorGUILayout.Space(8f);
                requestUpdate = DrawActions(dataTable);

                EditorGUILayout.Space(8f);
                _showRemoteSync = EditorGUILayout.Foldout(_showRemoteSync, "Remote Sync", true);
                if (_showRemoteSync)
                {
                    requestRemoteSync = DrawRemoteSync();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("This DataTable is not configured. Create DataTables through DataTableConfigure.", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();

            if (requestUpdate)
            {
                UpdateDataTableAsync(dataTable);
            }

            if (requestRemoteSync)
            {
                RemoteSyncAndRefreshAsync(dataTable);
            }

            static bool DrawActions(DataTable targetDataTable)
            {
                var requestedUpdate = false;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Update DataTable", GUILayout.Height(32f)))
                {
                    requestedUpdate = true;
                }

                if (GUILayout.Button("Clear Rows", GUILayout.Height(32f)))
                {
                    if (EditorUtility.DisplayDialog("Clear DataTable", $"Are you sure you want to clear all rows from '{targetDataTable.name}'?", "Clear", "Cancel"))
                    {
                        targetDataTable.ClearData();
                    }
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                return requestedUpdate;
            }

            bool DrawRemoteSync()
            {
                var requestedRemoteSync = false;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(_remoteCsvUrlProperty, new GUIContent("Remote CSV URL"));

                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_remoteCsvUrlProperty.stringValue)))
                {
                    if (GUILayout.Button("Remote Sync & Refresh", GUILayout.Height(30f)))
                    {
                        requestedRemoteSync = true;
                    }
                }

                EditorGUILayout.EndVertical();

                return requestedRemoteSync;
            }
        }

        private static async void UpdateDataTableAsync(DataTable dataTable)
        {
            if (!dataTable || !dataTable.dataTableCsv)
            {
                return;
            }

            await dataTable.UpdateData(dataTable.dataTableCsv, null);
        }

        private static async void RemoteSyncAndRefreshAsync(DataTable dataTable)
        {
            if (!dataTable)
            {
                return;
            }

            await RemoteDataTableSync.SyncAndRefresh(dataTable);
        }

        private static void DrawHeader(DataTable dataTable)
        {
            var rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), new Color(0.35f, 0.65f, 1f));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(dataTable.name, new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15
            });
            EditorGUILayout.LabelField("JxModule DataTable", EditorStyles.miniLabel);
            EditorGUILayout.Space(2f);

            EditorGUILayout.EndVertical();
        }

        private static void DrawSummary(DataTable dataTable)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
            DrawInfoRow("Row Type", dataTable.GetRowType()?.Name ?? "Missing");
            DrawInfoRow("CSV", dataTable.dataTableCsv ? dataTable.dataTableCsv.name : "Missing");
            DrawInfoRow("Rows", dataTable.dataTableRows.Count.ToString());

            EditorGUILayout.EndVertical();
        }

        private static void DrawTableFields(DataTable dataTable)
        {
            var targetType = dataTable.GetRowType();
            if (targetType == null)
            {
                EditorGUILayout.HelpBox("DataTable Row Type could not be found.", MessageType.Error);
                return;
            }

            var fields = targetType.GetFields(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly)
                .Where(IsSerializedField)
                .ToArray();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField(
                $"{targetType.Name} has {fields.Length} fields.",
                EditorStyles.boldLabel
            );

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Field", EditorStyles.toolbarButton, GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.45f));
            GUILayout.Label("Type", EditorStyles.toolbarButton);
            EditorGUILayout.EndHorizontal();

            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                var rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(22f));
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(rowRect, i % 2 == 0 ? new Color(0.19f, 0.19f, 0.19f) : new Color(0.15f, 0.15f, 0.15f));
                }

                GUILayout.Label(field.Name, GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.45f));
                GUILayout.Label(GetTypeName(field.FieldType));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawInfoRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel, GUILayout.Width(90f));
            EditorGUILayout.SelectableLabel(value, EditorStyles.label, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.EndHorizontal();
        }

        private static bool IsSerializedField(FieldInfo field)
        {
            if (field.IsStatic)
            {
                return false;
            }

            if (field.IsNotSerialized)
            {
                return false;
            }

            if (field.IsPublic)
            {
                return true;
            }

            return field.GetCustomAttribute<SerializeField>() != null;
        }

        private static void SyncCsv(DataTable dataTable)
        {
            if (!dataTable.dataTableCsv)
            {
                return;
            }

            if (dataTable.name == dataTable.dataTableCsv.name)
            {
                return;
            }

            var path = AssetDatabase.GetAssetPath(dataTable.dataTableCsv);

            AssetDatabase.RenameAsset(path, dataTable.name);

            EditorUtility.SetDirty(dataTable);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static string GetTypeName(Type type)
        {
            if (type == typeof(string))
            {
                return "string";
            }

            if (type == typeof(int))
            {
                return "int";
            }

            if (type == typeof(float))
            {
                return "float";
            }

            if (type == typeof(bool))
            {
                return "bool";
            }

            if (type == typeof(double))
            {
                return "double";
            }

            if (type.IsEnum)
            {
                return "enum";
            }

            if (type == typeof(Color))
            {
                return "Color";
            }

            if (type.IsGenericType)
            {
                var genericType = type.GetGenericTypeDefinition();
                var typeArgs = type.GetGenericArguments();

                if (genericType == typeof(List<>))
                {
                    return $"List<{GetTypeName(typeArgs[0])}>";
                }
            }

            return type.IsArray ? $"{GetTypeName(type.GetElementType())}[]" : type.Name;
        }

        [MenuItem("Assets/JxModule/Copy Datatable")]
        public static void CopyAsset()
        {
            var asset = Selection.activeObject as DataTable;
            if (asset != null)
            {
                asset.CopyAsset();
            }
        }

        [MenuItem("Assets/JxModule/Validate DataTable", true)]
        public static bool ValidateCopyAsset()
        {
            return Selection.activeObject is DataTable;
        }
    }
#endif
#endregion Editor

    public class DataTable : ScriptableObject
    {
        [ReadOnly] public TextAsset dataTableCsv;
        [ReadOnly] public string dataTableRowName;
        public string remoteCsvUrl;
        [HideInInspector] public List<DataTableRowBase> dataTableRows = new();

#if UNITY_EDITOR
        [ReadOnly] public MonoScript dataTableRowScript;

        public const string CsvPropertyName = nameof(dataTableCsv);
        public const string RowTypePropertyName = nameof(dataTableRowName);
        public const string RowScriptPropertyName = nameof(dataTableRowScript);
        public const string RemoteCsvUrlPropertyName = nameof(remoteCsvUrl);
        public const string DataTableRowsPropertyName = nameof(dataTableRows);
#endif

        public string GetRowName()
        {
            return dataTableRowName;
        }

        public List<T> Get<T>() where T : DataTableRowBase
        {
            return dataTableRows.ConvertAll<T>(x => x as T);
        }

        public T Find<T>(string rowID) where T : DataTableRowBase
        {
            return Get<T>().Find(x => x.rowID == rowID);
        }

        public T Find<T>(Predicate<T> predicate) where T : DataTableRowBase
        {
            return Get<T>().Find(predicate);
        }

        public List<T> FindAll<T>() where T : DataTableRowBase
        {
            return Get<T>();
        }

        public List<T> FindAll<T>(Predicate<T> predicate) where T : DataTableRowBase
        {
            return Get<T>().FindAll(predicate);
        }

#region Editor
#if UNITY_EDITOR
        public Type GetRowType()
        {
            if (dataTableRowScript)
            {
                var type = dataTableRowScript.GetClass();
                if (type != null)
                {
                    return type;
                }
            }

            if (!string.IsNullOrEmpty(dataTableRowName))
            {
                return Type.GetType(dataTableRowName);
            }

            return null;
        }

        public void CopyAsset()
        {
            var originAssetPath = AssetDatabase.GetAssetPath(this);
            var originCsvPath = AssetDatabase.GetAssetPath(dataTableCsv);

            if (string.IsNullOrEmpty(originAssetPath) ||
                string.IsNullOrEmpty(originCsvPath))
            {
                return;
            }

            var copyAssetPath = originAssetPath.Replace(".asset", "_copy.asset");
            var copyCsvPath = originCsvPath.Replace(".csv", "_copy.csv");

            if (!AssetDatabase.CopyAsset(originAssetPath, copyAssetPath))
            {
                Debug.LogError($"DataTable: Failed to copy DataTable: {originAssetPath}");
                return;
            }

            if (!AssetDatabase.CopyAsset(originCsvPath, copyCsvPath))
            {
                AssetDatabase.DeleteAsset(copyAssetPath);
                Debug.LogError($"DataTable: Failed to copy CSV: {originCsvPath}");
                return;
            }

            AssetDatabase.Refresh();

            var copyAsset = AssetDatabase.LoadAssetAtPath<DataTable>(copyAssetPath);
            var copyCsv = AssetDatabase.LoadAssetAtPath<TextAsset>(copyCsvPath);

            if (!copyAsset || !copyCsv)
            {
                return;
            }

            copyAsset.dataTableCsv = copyCsv;

            EditorUtility.SetDirty(copyAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = copyAsset;
            EditorGUIUtility.PingObject(copyAsset);
        }

        private static async Task<object> ProcessGenericType(Type fieldType, string rawValue)
        {
            var elementType = fieldType.GetGenericArguments()[0];

            var list = rawValue.SplitStringList();
            object result = null;

            if (elementType == typeof(string))
            {
                result = new List<string>(list.ConvertAll(x => x.ToString()));
            }
            else if (elementType == typeof(int))
            {
                result = list.ConvertAll(int.Parse);
            }
            else if (elementType == typeof(float))
            {
                result = list.ConvertAll(float.Parse);
            }
            else if (elementType == typeof(bool))
            {
                result = list.ConvertAll(bool.Parse);
            }
            else if (elementType == typeof(Color))
            {
                var colorList = new List<Color>();

                foreach (var item in list)
                {
                    if (ColorUtility.TryParseHtmlString(item, out var color))
                    {
                        colorList.Add(color);
                    }
                    else
                    {
                        Debug.LogError($"DataTable: Fail to process Color: {item}");
                    }
                }

                result = colorList;
            }
            else if (elementType.IsEnum)
            {
                var targetListType = typeof(List<>).MakeGenericType(elementType);
                var targetList = Activator.CreateInstance(targetListType);
                var addMethod = targetListType.GetMethod("Add");

                foreach (var item in list)
                {
                    addMethod?.Invoke(targetList, new object[] { Enum.Parse(elementType, item) });
                }

                result = targetList;
            }
            else if (typeof(MonoBehaviour).IsAssignableFrom(elementType))
            {
                var targetListType = typeof(List<>).MakeGenericType(elementType);
                var targetList = Activator.CreateInstance(targetListType);
                var addMethod = targetListType.GetMethod("Add");
                var loadedCount = 0;

                foreach (var item in list)
                {
                    if (string.IsNullOrWhiteSpace(item))
                    {
                        continue;
                    }

                    await AddressableExtension.LoadAsset<GameObject>(
                        item,
                        x =>
                        {
                            var component = x.GetComponent(elementType);

                            if (component == null)
                            {
                                Debug.LogWarning(
                                    $"DataTable: Component [{elementType.Name}] not found on addressable asset: {item}"
                                );

                                return;
                            }

                            addMethod?.Invoke(targetList, new object[] { component });
                            loadedCount++;
                        }
                    );
                }

                result = loadedCount > 0 ? targetList : null;
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(elementType))
            {
                var targetListType = typeof(List<>).MakeGenericType(elementType);
                var targetList = Activator.CreateInstance(targetListType);
                var addMethod = targetListType.GetMethod("Add");
                var loadedCount = 0;

                foreach (string item in list)
                {
                    if (string.IsNullOrWhiteSpace(item))
                    {
                        continue;
                    }

                    await AddressableExtension.LoadAsset<UnityEngine.Object>(
                        item,
                        x =>
                        {
                            if (!elementType.IsInstanceOfType(x))
                            {
                                Debug.LogWarning(
                                    $"DataTable: Loaded asset type [{x.GetType().Name}] is not assignable to [{elementType.Name}]: {item}"
                                );

                                return;
                            }

                            addMethod?.Invoke(targetList, new object[] { x });
                            loadedCount++;
                        }
                    );
                }

                result = loadedCount > 0 ? targetList : null;
            }

            return result;
        }

        public async Task UpdateData(TextAsset csv, Action<object, Dictionary<string, string>> customAction)
        {
            var validationResult = DataTableValidator.Validate(this);
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    Debug.LogError($"DataTable Validation: {error}");
                }

                Debug.LogError($"DataTable: Validation failed. Update aborted: {name}");
                return;
            }

            var rowType = GetRowType();
            if (rowType == null)
            {
                Debug.LogError($"DataTable: Row type not found: {name}");
                return;
            }

            if (!csv)
            {
                Debug.LogError($"DataTable: CSV not found: {name}");
                return;
            }

            dataTableRowName = rowType.AssemblyQualifiedName;

            var tableRows = new List<DataTableRowBase>();
            var csvContents = CsvReader.Read(csv);

            var rowCount = 1;

            foreach (var row in csvContents)
            {
                var isEnable = bool.Parse(row["isEnable"]);
                if (!isEnable)
                {
                    continue;
                }

                var targetRow = dataTableRows.Find(x => x.rowID == row["rowID"]);

                if (!targetRow)
                {
                    targetRow = CreateInstance(rowType) as DataTableRowBase;

                    if (!targetRow)
                    {
                        Debug.LogError($"DataTable: Failed to create row type: {rowType.Name}");
                        continue;
                    }

                    AssetDatabase.AddObjectToAsset(targetRow, this);
                    EditorUtility.SetDirty(this);
                }

                targetRow.SetRawData(row);

                var allFields = rowType.GetFields(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance
                );

                foreach (var field in allFields)
                {
                    if (field.IsStatic)
                    {
                        continue;
                    }

                    if (field.IsNotSerialized)
                    {
                        continue;
                    }

                    if (!field.IsPublic &&
                        field.GetCustomAttribute<SerializeField>() == null)
                    {
                        continue;
                    }

                    var key = row.Keys.FirstOrDefault(
                        k => k.Equals(field.Name, StringComparison.OrdinalIgnoreCase)
                    );

                    if (key == null)
                    {
                        Debug.LogWarning($"DataTable: Column not found [{field.Name}] in {name}.");
                        continue;
                    }

                    var rawValue = row[key];
                    var fieldType = field.FieldType;

                    try
                    {
                        object parsedValue;

                        if (fieldType.IsGenericType)
                        {
                            parsedValue = await ProcessGenericType(fieldType, rawValue);

                            if (parsedValue != null)
                            {
                                field.SetValue(targetRow, parsedValue);
                            }
                        }
                        else
                        {
                            if (fieldType == typeof(string))
                            {
                                parsedValue = rawValue;
                                field.SetValue(targetRow, parsedValue);
                            }
                            else if (fieldType == typeof(int))
                            {
                                parsedValue = int.Parse(rawValue);
                                field.SetValue(targetRow, parsedValue);
                            }
                            else if (fieldType == typeof(float))
                            {
                                parsedValue = float.Parse(rawValue);
                                field.SetValue(targetRow, parsedValue);
                            }
                            else if (fieldType == typeof(bool))
                            {
                                parsedValue = bool.Parse(rawValue);
                                field.SetValue(targetRow, parsedValue);
                            }
                            else if (fieldType == typeof(double))
                            {
                                parsedValue = double.Parse(rawValue);
                                field.SetValue(targetRow, parsedValue);
                            }
                            else if (fieldType == typeof(Color))
                            {
                                if (ColorUtility.TryParseHtmlString(rawValue, out var color))
                                {
                                    parsedValue = color;
                                    field.SetValue(targetRow, parsedValue);
                                }
                                else
                                {
                                    Debug.LogWarning($"DataTable: Color parse failed: {rawValue}");
                                }
                            }
                            else if (fieldType.IsEnum)
                            {
                                parsedValue = Enum.Parse(fieldType, rawValue);
                                field.SetValue(targetRow, parsedValue);
                            }
                            else if (typeof(MonoBehaviour).IsAssignableFrom(fieldType))
                            {
                                if (string.IsNullOrWhiteSpace(rawValue))
                                {
                                    continue;
                                }

                                await AddressableExtension.LoadAsset<GameObject>(
                                    rawValue,
                                    x =>
                                    {
                                        parsedValue = x.GetComponent(fieldType);

                                        if (parsedValue != null)
                                        {
                                            field.SetValue(targetRow, parsedValue);
                                        }
                                        else
                                        {
                                            Debug.LogWarning($"DataTable: Component [{fieldType.Name}] not found on addressable asset: {rawValue}");
                                        }
                                    }
                                );
                            }
                            else if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
                            {
                                if (string.IsNullOrWhiteSpace(rawValue))
                                {
                                    continue;
                                }

                                await AddressableExtension.LoadAsset<UnityEngine.Object>(
                                    rawValue,
                                    x =>
                                    {
                                        if (fieldType.IsInstanceOfType(x))
                                        {
                                            field.SetValue(targetRow, x);
                                        }
                                        else
                                        {
                                            Debug.LogWarning($"DataTable: Loaded asset type [{x.GetType().Name}] is not assignable to [{fieldType.Name}]: {rawValue}");
                                        }
                                    }
                                );
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"DataTable: Fail to update [{name}]-[Column : {field.Name}, Row : {rowCount}] - {e.Message}");
                    }
                }

                customAction?.Invoke(targetRow, row);

                targetRow.name = targetRow.rowID;

                EditorUtility.SetDirty(targetRow);
                EditorUtility.SetDirty(this);

                tableRows.Add(targetRow);

                rowCount++;
            }

            var path = AssetDatabase.GetAssetPath(this);
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);

            foreach (var asset in allAssets)
            {
                if (asset == this)
                {
                    continue;
                }

                if (asset is not DataTableRowBase oldRow)
                {
                    continue;
                }

                var existsInLatest = tableRows.Exists(
                    newRow => newRow.rowID == oldRow.rowID
                );

                if (!existsInLatest ||
                    string.IsNullOrEmpty(oldRow.rowID) ||
                    string.IsNullOrEmpty(oldRow.name))
                {
                    DestroyImmediate(oldRow, true);
                }
            }

            dataTableRows = tableRows;

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            DebugExtension.LogColor(
                "DataTable: Successfully update DataTable.",
                Color.green
            );
        }

        public void ClearData()
        {
            var path = AssetDatabase.GetAssetPath(this);
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);

            foreach (var asset in allAssets)
            {
                if (asset is not DataTableRowBase tableRow)
                {
                    continue;
                }

                DestroyImmediate(tableRow, true);
            }

            dataTableRows.Clear();

            SaveChanges();

            DebugExtension.LogColor(
                "DataTable: Successfully clear DataTable.",
                Color.green
            );
        }

        private static bool IsSerializedField(FieldInfo field)
        {
            if (field.IsStatic)
            {
                return false;
            }

            if (field.IsNotSerialized)
            {
                return false;
            }

            if (field.IsPublic)
            {
                return true;
            }

            return field.GetCustomAttribute<SerializeField>() != null;
        }

        private void SaveChanges()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
#endif
#endregion Editor
    }
}
