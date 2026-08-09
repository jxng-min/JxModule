using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace JxModule
{
    public static class SaveSystem
    {
        public static void Save<T>(T data,
                                   string fileName,
                                   string directory,
                                   bool prettyPrint = false) where T : ISerializeData
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var directoryPath = GetDirectoryPath(directory);
            var savePath = GetSavePath(fileName, directory);

            Directory.CreateDirectory(directoryPath);

            var jsonString = JsonUtility.ToJson(data, prettyPrint);

            using var fileStream = File.Open(savePath, FileMode.Create, FileAccess.Write);
            using var binaryWriter = new BinaryWriter(fileStream, Encoding.UTF8);

            binaryWriter.Write(jsonString);

#if UNITY_EDITOR
            Debug.Log($"{fileName} is saved!");
            Debug.Log(savePath);
#endif
        }

        public static T Load<T>(string fileName, string directory) where T : ISerializeData
        {
            var savePath = GetSavePath(fileName, directory);

            if (!File.Exists(savePath))
            {
                throw new FileNotFoundException($"No save file found: {savePath}", savePath);
            }

            var data = LoadFile<T>(savePath);

#if UNITY_EDITOR
            Debug.Log($"{fileName} is loaded!");
            Debug.Log(savePath);
#endif

            return data;
        }

        public static void Load<T>(string fileName, string directory, out T data) where T : ISerializeData
        {
            data = Load<T>(fileName, directory);
        }

        public static bool TryLoad<T>(string fileName, string directory, out T data) where T : ISerializeData
        {
            var savePath = GetSavePath(fileName, directory);

            if (!File.Exists(savePath))
            {
                data = default;
                return false;
            }

            try
            {
                data = LoadFile<T>(savePath);
                return data != null;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to load save file: {savePath}\n{exception}");
                data = default;
                return false;
            }
        }

        public static List<T> LoadAll<T>(string directory, string searchPattern = "*") where T : ISerializeData
        {
            var directoryPath = GetDirectoryPath(directory);

            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"Directory doesn't exist: {directoryPath}");
            }

            var filePaths = Directory.GetFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly);

            if (filePaths.Length == 0)
            {
                throw new FileNotFoundException($"No save files found in directory: {directoryPath}");
            }

            var dataList = new List<T>();

            foreach (var filePath in filePaths)
            {
                try
                {
                    var data = LoadFile<T>(filePath);

                    if (data != null)
                    {
                        dataList.Add(data);
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Failed to load save file: {filePath}\n{exception}");
                }
            }

            return dataList;
        }

        public static void LoadAll<T>(string directory, out List<T> dataList, string searchPattern = "*") where T : ISerializeData
        {
            dataList = LoadAll<T>(directory, searchPattern);
        }

        public static void Remove(string fileName, string directory)
        {
            var savePath = GetSavePath(fileName, directory);

            if (!File.Exists(savePath))
            {
                return;
            }

            File.Delete(savePath);

#if UNITY_EDITOR
            Debug.Log($"{fileName} is removed!");
            Debug.Log(savePath);
#endif
        }

        public static void RemoveAll(string directory)
        {
            var directoryPath = GetDirectoryPath(directory);

            if (!Directory.Exists(directoryPath))
            {
                return;
            }

            Directory.Delete(directoryPath, true);

#if UNITY_EDITOR
            Debug.Log("Save directory is removed!");
            Debug.Log(directoryPath);
#endif
        }

        public static bool Exist(string fileName, string directory)
        {
            return File.Exists(GetSavePath(fileName, directory));
        }

        public static bool DirectoryExist(string directory)
        {
            return Directory.Exists(GetDirectoryPath(directory));
        }

        private static T LoadFile<T>(string filePath) where T : ISerializeData
        {
            using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var binaryReader = new BinaryReader(fileStream, Encoding.UTF8);

            var jsonString = binaryReader.ReadString();
            return JsonUtility.FromJson<T>(jsonString);
        }

        private static string GetSavePath(string fileName, string directory)
        {
            return Path.Combine(Application.persistentDataPath, directory, fileName);
        }

        private static string GetDirectoryPath(string directory)
        {
            return Path.Combine(Application.persistentDataPath, directory);
        }
    }
}