using System.Collections.Generic;
using UnityEngine;

namespace JxModule
{
    public class JxObjectPool : MonoBehaviour
    {
        private GameObject _objectPrefab;
        private int _initialPoolSize;
        private int _maxPoolSize;
        private bool _isExpandable;
        
        private readonly Queue<GameObject> _objectPool = new();
        private readonly HashSet<GameObject> _activeObjects = new();
        private readonly HashSet<GameObject> _allObjects = new();
        
        public int PooledCount => _objectPool.Count;
        public int ActiveCount => _activeObjects.Count;
        public int TotalCount => _allObjects.Count;
        
#if UNITY_EDITOR
        public enum EPoolStatus
        {
            NotEnoughData,
            Oversized,
            Healthy,
            Tight,
            Undersized
        }
        
        private const float RecommendedSafetyMultiplier = 1.1f;
        private const float OversizedThreshold = 0.35f;
        private const float TightThreshold = 0.85f;
        
        private const float HistorySampleInterval = 0.1f;
        private const int MaxHistoryCount = 300;
        
        private float _activeCountSum;
        private int _activeCountSampleCount;
        
        private float _lifetimeSum;
        private int _lifetimeSampleCount;
        
        private float _nextHistorySampleTime;
        
        private readonly Dictionary<GameObject, float> _activeStartTimes = new();
        private readonly Queue<int> _activeCountHistory = new();
        
        public string PoolName => _objectPrefab != null ? _objectPrefab.name : name;
        
        public GameObject Prefab => _objectPrefab;
        public int InitialPoolSize => _initialPoolSize;
        public int MaxPoolSize => _maxPoolSize;
        public bool IsExpandable => _isExpandable;
        
        public int PeakActiveCount { get; private set; }
        
        public int GetCount { get; private set; }
        public int ReturnCount { get; private set; }
        public int ReuseCount { get; private set; }
        
        public int CreatedCount { get; private set; }
        public int ExpandedCreateCount { get; private set; }
        public int MissCount { get; private set; }
        public int DestroyedReferenceCount { get; private set; }
        
        public float AverageActiveCount => _activeCountSampleCount == 0 ? 0f : _activeCountSum / _activeCountSampleCount;
        public float AverageLifetime => _lifetimeSampleCount == 0 ? 0f : _lifetimeSum / _lifetimeSampleCount;
        public float MaxLifetime { get; private set; }
        public float ReuseRate => GetCount == 0 ? 0f : (float)ReuseCount / GetCount;
        public float ExpansionRate => GetCount == 0 ? 0f : (float)ExpandedCreateCount / GetCount;
        public float MissRate => GetCount == 0 ? 0f : (float)MissCount / GetCount;
        public float UtilizationRate => _initialPoolSize <= 0 ? 0f : (float)PeakActiveCount / _initialPoolSize;
        
        public int RecommendedPoolSize
        {
            get
            {
                if (PeakActiveCount <= 0)
                {
                    return _initialPoolSize;
                }
                
                return Mathf.CeilToInt(PeakActiveCount * RecommendedSafetyMultiplier);
            }
        }
        
        public EPoolStatus Status
        {
            get
            {
                if (GetCount == 0)
                {
                    return EPoolStatus.NotEnoughData;
                }
                
                if (ExpandedCreateCount > 0 || MissCount > 0)
                {
                    return EPoolStatus.Undersized;
                }
                
                if (_initialPoolSize <= 0)
                {
                    return EPoolStatus.NotEnoughData;
                }
                
                if (UtilizationRate < OversizedThreshold)
                {
                    return EPoolStatus.Oversized;
                }
                
                if (UtilizationRate >= TightThreshold)
                {
                    return EPoolStatus.Tight;
                }
                
                return EPoolStatus.Healthy;
            }
        }
        
        public IReadOnlyCollection<GameObject> ActiveObjects => _activeObjects;
        public IReadOnlyCollection<int> ActiveCountHistory => _activeCountHistory;
#endif
        
        public void Initialize(GameObject prefab, int initialPoolSize, int maxPoolSize, bool isExpandable)
        {
            _objectPrefab = prefab;
            _initialPoolSize = initialPoolSize;
            _maxPoolSize = maxPoolSize;
            _isExpandable = isExpandable;
            
            if (_objectPrefab == null)
            {
                Debug.LogError("JxObjectPool: Prefab is not assigned.", this);
                return;
            }
            
            InitializePool();
        }
        
        public GameObject Get()
        {
            CleanupDestroyedObjects();
            
#if UNITY_EDITOR
            GetCount++;
#endif
            
            GameObject obj = null;
            
            while (_objectPool.Count > 0)
            {
                obj = _objectPool.Dequeue();
                
                if (obj == null)
                {
#if UNITY_EDITOR
                    DestroyedReferenceCount++;
#endif
                    continue;
                }
                
#if UNITY_EDITOR
                ReuseCount++;
#endif
                
                break;
            }
            
            if (obj == null)
            {
                if (!_isExpandable)
                {
#if UNITY_EDITOR
                    MissCount++;
#endif
                    
                    return null;
                }
                
                obj = CreateExpandedObject();
            }
            
            if (obj == null)
            {
                return null;
            }
            
            obj.SetActive(true);
            _activeObjects.Add(obj);
            
#if UNITY_EDITOR
            PeakActiveCount = Mathf.Max(PeakActiveCount, ActiveCount);
            _activeStartTimes[obj] = Time.unscaledTime;
#endif
            
            return obj;
        }
        
        public void Return(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }
            
            if (!_activeObjects.Contains(obj))
            {
                return;
            }
            
#if UNITY_EDITOR
            ReturnCount++;
            RecordLifetime(obj);
#endif
            
            _activeObjects.Remove(obj);
            
            if (obj == null)
            {
                CleanupDestroyedObjects();
                return;
            }
            
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            
            if (_objectPool.Count < _maxPoolSize)
            {
                _objectPool.Enqueue(obj);
            }
            else
            {
                _allObjects.Remove(obj);
                Destroy(obj);
            }
        }
        
        public void ReturnAll()
        {
            CleanupDestroyedObjects();
            
            var activeObjects = new List<GameObject>(_activeObjects);
            
            foreach (var obj in activeObjects)
            {
                if (obj == null)
                {
                    continue;
                }
                
                Return(obj);
            }
        }
        
        public void Clear()
        {
            ReturnAll();
            
            while (_objectPool.Count > 0)
            {
                var obj = _objectPool.Dequeue();
                
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            
            _objectPool.Clear();
            _activeObjects.Clear();
            _allObjects.Clear();
            
#if UNITY_EDITOR
            _activeStartTimes.Clear();
            _activeCountHistory.Clear();
#endif
        }
        
        private void InitializePool()
        {
            for (var i = 0; i < _initialPoolSize; i++)
            {
                CreatePooledObject();
            }
        }
        
        private GameObject CreatePooledObject()
        {
            if (_objectPrefab == null)
            {
                return null;
            }
            
            var obj = Instantiate(_objectPrefab, transform);
            
            obj.SetActive(false);
            
            _allObjects.Add(obj);
            _objectPool.Enqueue(obj);
            
#if UNITY_EDITOR
            CreatedCount++;
#endif
            
            return obj;
        }
        
        private GameObject CreateExpandedObject()
        {
            if (_objectPrefab == null)
            {
                return null;
            }
            
            var obj = Instantiate(_objectPrefab, transform);
            
            obj.SetActive(false);
            
            _allObjects.Add(obj);
            
#if UNITY_EDITOR
            CreatedCount++;
            ExpandedCreateCount++;
#endif
            
            return obj;
        }
        
        private void CleanupDestroyedObjects()
        {
            _activeObjects.RemoveWhere(obj => obj == null);
            _allObjects.RemoveWhere(obj => obj == null);
            
            if (_objectPool.Count == 0)
            {
                return;
            }
            
            var pooledCount = _objectPool.Count;
            
            for (var i = 0; i < pooledCount; i++)
            {
                var obj = _objectPool.Dequeue();
                
                if (obj == null)
                {
#if UNITY_EDITOR
                    DestroyedReferenceCount++;
#endif
                    continue;
                }
                
                _objectPool.Enqueue(obj);
            }
            
#if UNITY_EDITOR
            var destroyedActiveObjects = new List<GameObject>();
            
            foreach (var pair in _activeStartTimes)
            {
                if (pair.Key == null)
                {
                    destroyedActiveObjects.Add(pair.Key);
                }
            }
            
            foreach (var obj in destroyedActiveObjects)
            {
                _activeStartTimes.Remove(obj);
            }
#endif
        }
        
#if UNITY_EDITOR
        public float GetActiveLifetime(GameObject obj)
        {
            if (obj == null)
            {
                return 0f;
            }
            
            if (!_activeStartTimes.TryGetValue(obj, out var startTime))
            {
                return 0f;
            }
            
            return Time.unscaledTime - startTime;
        }
        
        public void ResetProfiler()
        {
            CleanupDestroyedObjects();
            
            PeakActiveCount = ActiveCount;
            
            GetCount = 0;
            ReturnCount = 0;
            ReuseCount = 0;
            
            CreatedCount = 0;
            ExpandedCreateCount = 0;
            MissCount = 0;
            DestroyedReferenceCount = 0;
            
            _activeCountSum = 0f;
            _activeCountSampleCount = 0;
            
            _lifetimeSum = 0f;
            _lifetimeSampleCount = 0;
            
            MaxLifetime = 0f;
            
            _activeStartTimes.Clear();
            _activeCountHistory.Clear();
            
            _nextHistorySampleTime = Time.unscaledTime;
            
            foreach (var obj in _activeObjects)
            {
                if (obj == null)
                {
                    continue;
                }
                
                _activeStartTimes[obj] = Time.unscaledTime;
            }
        }
        
        private void RecordLifetime(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }
            
            if (!_activeStartTimes.Remove(obj, out var startTime))
            {
                return;
            }
            
            var lifetime = Time.unscaledTime - startTime;
            _lifetimeSum += lifetime;
            _lifetimeSampleCount++;
            
            MaxLifetime = Mathf.Max(MaxLifetime, lifetime);
        }
        
        private void RecordActiveHistory()
        {
            if (Time.unscaledTime < _nextHistorySampleTime)
            {
                return;
            }
            
            _nextHistorySampleTime = Time.unscaledTime + HistorySampleInterval;
            _activeCountHistory.Enqueue(ActiveCount);
            
            while (_activeCountHistory.Count > MaxHistoryCount)
            {
                _activeCountHistory.Dequeue();
            }
        }
        
        private void Update()
        {
            _activeCountSum += ActiveCount;
            _activeCountSampleCount++;
            
            RecordActiveHistory();
        }
#endif
        
        private void OnDestroy()
        {
            Clear();
        }
    }
}