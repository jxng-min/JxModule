using System;

namespace JxModule.Terminal
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class JxCommandAttribute : Attribute
    {
        public string Key { get; }
        public string Description { get; set; }

        public JxCommandAttribute() { }

        public JxCommandAttribute(string key)
        {
            Key = key;
        }
    }
}
