#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace JxModule.DataTable
{
    public class DataTableRowSelectionWindow : EditorWindow
    {
        private static readonly Color AccentColor = new(0.35f, 0.65f, 1f);

        private Action<MonoScript> _onScriptSelected;
        private Vector2 _scrollPosition;
        private string _searchText = "";

        public static void Open(Action<MonoScript> onScriptSelected)
        {
            var window = CreateInstance<DataTableRowSelectionWindow>();

            window.titleContent = new GUIContent("Select DataTable Row");
            window._onScriptSelected = onScriptSelected;
            window.minSize = new Vector2(360f, 420f);
            window.ShowUtility();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawSearchField();
            DrawRowScriptList();
        }

        private void DrawHeader()
        {
            var rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), AccentColor);

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Select DataTable Row", new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15
            });
            EditorGUILayout.LabelField("Choose a DataTableRowBase script type.", EditorStyles.miniLabel);
            EditorGUILayout.Space(2f);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
        }

        private void DrawSearchField()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _searchText = EditorGUILayout.TextField("Search", _searchText);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private void DrawRowScriptList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            IEnumerable<Type> rowTypes = TypeCache.GetTypesDerivedFrom<DataTableRowBase>()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .OrderBy(t => t.Name);

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                rowTypes = rowTypes.Where(t => t.Name.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            var hasAny = false;

            foreach (var rowType in rowTypes)
            {
                var script = FindMonoScript(rowType);
                if (script == null)
                {
                    continue;
                }

                hasAny = true;

                if (GUILayout.Button(rowType.Name, GUILayout.Height(28f)))
                {
                    _onScriptSelected?.Invoke(script);
                    Close();
                }
            }

            if (!hasAny)
            {
                EditorGUILayout.HelpBox("No matching row scripts found.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private static MonoScript FindMonoScript(Type type)
        {
            var assetGuids = AssetDatabase.FindAssets($"{type.Name} t:MonoScript");

            foreach (var guid in assetGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);

                if (script != null && script.GetClass() == type)
                {
                    return script;
                }
            }

            return null;
        }
    }
}
#endif
