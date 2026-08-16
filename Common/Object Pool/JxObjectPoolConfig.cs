using System;
using UnityEngine;

namespace JxModule
{
    public class JxObjectPoolConfig : ScriptableObject
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int initialPoolSize;
        [SerializeField] private int maxPoolSize;
        [SerializeField] private bool isExpandable = false;
        
        public GameObject Prefab => prefab;
        public int InitialPoolSize => initialPoolSize;
        public int MaxPoolSize => maxPoolSize;
        public bool IsExpandable => isExpandable;
    }
}