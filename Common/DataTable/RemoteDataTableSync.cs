#if UNITY_EDITOR
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace JxModule.DataTable
{
    public static class RemoteDataTableSync
    {
        public static async Task SyncAndRefresh(DataTable dataTable)
        {
            var syncedCsv = await Sync(dataTable);

            if (syncedCsv == null)
            {
                return;
            }

            dataTable.dataTableCsv = syncedCsv;

            EditorUtility.SetDirty(dataTable);

            await dataTable.UpdateData(syncedCsv, null);
        }

        private static async Task<TextAsset> Sync(DataTable dataTable)
        {
            if (dataTable == null)
            {
                Debug.LogError("Remote DataTable Sync: DataTable is null.");
                return null;
            }

            if (string.IsNullOrEmpty(dataTable.remoteCsvUrl))
            {
                Debug.LogError($"Remote DataTable Sync: Remote CSV URL is empty. [{dataTable.name}]");

                return null;
            }

            if (dataTable.dataTableCsv == null)
            {
                Debug.LogError($"Remote DataTable Sync: CSV asset is null. [{dataTable.name}]");

                return null;
            }

            var csvPath = AssetDatabase.GetAssetPath(dataTable.dataTableCsv);

            if (string.IsNullOrEmpty(csvPath))
            {
                Debug.LogError($"Remote DataTable Sync: Can not found CSV asset's path. [{dataTable.name}]");

                return null;
            }

            using var request = UnityWebRequest.Get(dataTable.remoteCsvUrl);

            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Remote DataTable Sync: Failed to download CSV. " + $"[{dataTable.name}] - {request.error}");

                return null;
            }

            try
            {
                File.WriteAllText(csvPath, request.downloadHandler.text);
            }
            catch (IOException exception)
            {
                Debug.LogError($"Remote DataTable Sync: Failed to write CSV file. " + $"[{dataTable.name}] - {exception.Message}");

                return null;
            }

            AssetDatabase.ImportAsset(csvPath, ImportAssetOptions.ForceUpdate);

            var syncedCsv = AssetDatabase.LoadAssetAtPath<TextAsset>(csvPath);

            if (syncedCsv == null)
            {
                Debug.LogError($"Remote DataTable Sync: Failed to reload CSV asset. [{dataTable.name}]");

                return null;
            }

            DebugExtension.LogColor($"Remote DataTable Sync: Successfully sync CSV. [{dataTable.name}]", Color.green);

            return syncedCsv;
        }
    }
}
#endif