using System.Collections.Generic;
using UnityEngine;

namespace JxModule
{
    [CreateAssetMenu(fileName = "Object Pool Configure", menuName = "JxModule/Object Pool/Object Pool Configure")]
    public class ObjectPoolConfigure : ScriptableObject
    {
        [SerializeField] private List<JxObjectPoolConfig> configs = new();
        public IReadOnlyList<JxObjectPoolConfig> Configs => configs;
    }
}