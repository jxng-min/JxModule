using System.IO;
using UnityEditor;
using UnityEngine;

namespace JxModule
{
    public static class AssetExtension
    {
#if UNITY_EDITOR
        public static T CreateAsset<T>(string name) where T : ScriptableObject
        {
            var path = GetSelectedAssetPath();
            path = !string.IsNullOrEmpty(path) ? path : "Assets";

            var fullPath = Path.Combine(path, $"{name}.asset");
            var uniquePath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
            
            var asset = ScriptableObject.CreateInstance<T>();
            if (asset == null)
            {
                Debug.LogError($"Fail to create{typeof(T).Name} instance.");
                return null;
            }
            
            AssetDatabase.CreateAsset(asset, uniquePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            
            DebugExtension.LogColor($"Successfully create {typeof(T).Name} instance.", Color.green);
            return asset;
        }

        public static string GetSelectedAssetPath()
        {
            var selectedObject = Selection.activeObject;
            if (selectedObject == null)
            {
                return null;
            }

            var selectedPath = AssetDatabase.GetAssetPath(selectedObject);
            if (string.IsNullOrEmpty(selectedPath))
            {
                return null;
            }
            
            return AssetDatabase.IsValidFolder(selectedPath) ? selectedPath
                                                             : Path.GetDirectoryName(selectedPath);
        }
#endif
    }
}