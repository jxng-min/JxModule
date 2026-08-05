using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace JxModule
{
    public static class AddressableExtension
    {
        public static async Task<AsyncOperationHandle<T>> LoadAsset<T>(string key, Action<T> onSuccess, Action onFail = null)
        {
            var handle = Addressables.LoadAssetAsync<T>(key);
            await handle.Task;
            
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                onSuccess?.Invoke(handle.Result);
            }
            else
            {
                UnityEngine.Debug.LogError($"AddressableExtension: Fail to load {key}: {handle.OperationException?.Message}");
                onFail?.Invoke();
            }
            
            return handle;
        }
        
        public static async Task<AsyncOperationHandle<IList<T>>> LoadAssets<T>(string key, Action<T> onEachSuccess,
            Action<IList<T>> onSuccess = null, Action onFail = null)
        {
            var handle = Addressables.LoadAssetsAsync<T>(key, onEachSuccess);
            await handle.Task;
            
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                UnityEngine.Debug.LogError($"AddressableExtension: Fail to load {key}: {handle.OperationException?.Message}");
                onFail?.Invoke();
            }
            else
            {
                onSuccess?.Invoke(handle.Result);
            }
            
            return handle;
        }
    }
}
