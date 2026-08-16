using System.Collections.Generic;
using UnityEngine;

namespace JxModule
{
    public class ObjectPoolManager : GlobalSingleton<ObjectPoolManager>
    {
        [SerializeField] private ObjectPoolConfigure poolConfigure;
        
        private readonly Dictionary<GameObject, JxObjectPool> _poolDict = new();
        private readonly Dictionary<GameObject, GameObject> _instanceToPrefabDict = new();
        
        private Transform _poolParent;
        
#if UNITY_EDITOR
        public IReadOnlyDictionary<GameObject, JxObjectPool> Pools => _poolDict;
#endif
        
        protected override void Awake()
        {
            base.Awake();
            
            InitializePoolParent();
            InitializePools();
        }
        
        public GameObject Get(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogWarning("ObjectPoolManager: Prefab is null.", this);
                return null;
            }
            
            if (!_poolDict.TryGetValue(prefab, out var pool))
            {
                pool = CreateDefaultPool(prefab);
            }
            
            var instance = pool.Get();
            if (instance == null)
            {
                return null;
            }
            
            _instanceToPrefabDict[instance] = prefab;
            
            return instance;
        }
        
        public T Get<T>(T prefab) where T : Component
        {
            if (prefab == null)
            {
                Debug.LogWarning("ObjectPoolManager: Prefab is null.", this);
                return null;
            }
            
            var instance = Get(prefab.gameObject);
            if (instance == null)
            {
                return null;
            }
            
            return instance.GetComponent<T>();
        }
        
        public void Return(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }
            
            if (!_instanceToPrefabDict.TryGetValue(instance, out var prefab))
            {
                Debug.LogWarning($"ObjectPoolManager: '{instance.name}' is not managed by ObjectPoolManager.", instance);
                
                Destroy(instance);
                return;
            }
            
            if (!_poolDict.TryGetValue(prefab, out var pool))
            {
                Debug.LogWarning($"ObjectPoolManager: Pool for '{prefab.name}' does not exist.", instance);
                
                _instanceToPrefabDict.Remove(instance);
                Destroy(instance);
                return;
            }
            
            pool.Return(instance);
            
            _instanceToPrefabDict.Remove(instance);
        }
        
        public void Return(Component instance)
        {
            if (instance == null)
            {
                return;
            }
            
            Return(instance.gameObject);
        }
        
        public void ReturnSpecificPoolAll(Component prefab)
        {
            if (prefab == null)
            {
                return;
            }
    
            ReturnSpecificPoolAll(prefab.gameObject);
        }
        
        public void ReturnSpecificPoolAll(GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }
    
            if (!_poolDict.TryGetValue(prefab, out var pool))
            {
                Debug.LogWarning($"ObjectPoolManager: Pool for '{prefab.name}' does not exist.", prefab);
        
                return;
            }
    
            pool.ReturnAll();
    
            var removeTargets = new List<GameObject>();
            foreach (var pair in _instanceToPrefabDict)
            {
                if (pair.Value == prefab)
                {
                    removeTargets.Add(pair.Key);
                }
            }
    
            foreach (var instance in removeTargets)
            {
                _instanceToPrefabDict.Remove(instance);
            }
        }
        
        public void ReturnAll()
        {
            foreach (var pool in _poolDict.Values)
            {
                pool.ReturnAll();
            }
            
            _instanceToPrefabDict.Clear();
        }
        
        public bool HasPool(GameObject prefab)
        {
            if (prefab == null)
            {
                return false;
            }
            
            return _poolDict.ContainsKey(prefab);
        }
        
        public JxObjectPool GetPool(GameObject prefab)
        {
            if (prefab == null)
            {
                return null;
            }
            
            _poolDict.TryGetValue(prefab, out var pool);
            
            return pool;
        }
        
        private void InitializePoolParent()
        {
            var poolParentObject = new GameObject("Object Pools");
            
            poolParentObject.transform.SetParent(transform);
            poolParentObject.transform.localPosition = Vector3.zero;
            poolParentObject.transform.localRotation = Quaternion.identity;
            
            _poolParent = poolParentObject.transform;
        }
        
        private void InitializePools()
        {
            if (poolConfigure == null)
            {
                Debug.LogWarning("ObjectPoolManager: Object Pool Configure is not assigned.", this);
                return;
            }
            
            foreach (var config in poolConfigure.Configs)
            {
                if (!ValidateConfig(config))
                {
                    continue;
                }
                
                CreatePool(config);
            }
        }
        
        private bool ValidateConfig(JxObjectPoolConfig config)
        {
            if (config == null)
            {
                Debug.LogWarning("ObjectPoolManager: Null Object Pool Config detected.", this);
                return false;
            }
            
            if (config.Prefab == null)
            {
                Debug.LogWarning($"ObjectPoolManager: '{config.name}' has no prefab assigned.", config);
                return false;
            }
            
            if (_poolDict.ContainsKey(config.Prefab))
            {
                Debug.LogError($"ObjectPoolManager: Duplicate prefab detected. '{config.Prefab.name}'", config);
                return false;
            }
            
            if (config.InitialPoolSize < 0)
            {
                Debug.LogError($"ObjectPoolManager: '{config.name}' has invalid Initial Pool Size.", config);
                return false;
            }
            
            if (config.MaxPoolSize <= 0)
            {
                Debug.LogError($"ObjectPoolManager: '{config.name}' has invalid Max Pool Size.", config);
                return false;
            }
            
            if (config.InitialPoolSize > config.MaxPoolSize)
            {
                Debug.LogError($"ObjectPoolManager: '{config.name}' Initial Pool Size cannot exceed Max Pool Size.", config);
                return false;
            }
            
            return true;
        }
        
        private JxObjectPool CreatePool(JxObjectPoolConfig config)
        {
            var poolObject = new GameObject($"Pool_{config.Prefab.name}");
            
            poolObject.transform.SetParent(_poolParent);
            poolObject.transform.localPosition = Vector3.zero;
            poolObject.transform.localRotation = Quaternion.identity;
            
            var pool = poolObject.AddComponent<JxObjectPool>();
            
            pool.Initialize(
                config.Prefab,
                config.InitialPoolSize,
                config.MaxPoolSize,
                config.IsExpandable
            );
            
            _poolDict.Add(config.Prefab, pool);
            
            return pool;
        }
        
        private JxObjectPool CreateDefaultPool(GameObject prefab)
        {
            Debug.LogWarning($"ObjectPoolManager: '{prefab.name}' is not registered in Object Pool Configure. Creating default pool.", prefab);
            
            var poolObject = new GameObject($"Pool_{prefab.name}");
            
            poolObject.transform.SetParent(_poolParent);
            poolObject.transform.localPosition = Vector3.zero;
            poolObject.transform.localRotation = Quaternion.identity;
            
            var pool = poolObject.AddComponent<JxObjectPool>();
            pool.Initialize(prefab, 5, 25, true);
            _poolDict.Add(prefab, pool);
            
            return pool;
        }
    }
}