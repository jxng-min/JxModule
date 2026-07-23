#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace JxModule
{
    public sealed class ManagedEffectEntry
    {
        public MonoBehaviour target;
        public ManagedEffectAttribute attribute;
        public Editor inspector;
        public bool isExpanded;
        public string assetPath;
    }
}
#endif