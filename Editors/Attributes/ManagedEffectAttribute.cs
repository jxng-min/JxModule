using System;
using System.Diagnostics;

namespace JxModule
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ManagedEffectAttribute : Attribute
    {
        public string Category { get; }
        public string Name { get; }
        public int Order { get; }
        
        public ManagedEffectAttribute(string category, string name, int order = 0)
        {
            Category = category;
            Name = name;
            Order = order;
        }
    }
}