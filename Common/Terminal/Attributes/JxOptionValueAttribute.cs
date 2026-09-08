using System;

namespace JxModule.Terminal
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class JxOptionValueAttribute : Attribute
    {
        public string ProviderKey { get; }

        public JxOptionValueAttribute(string providerKey)
        {
            ProviderKey = providerKey;
        }
    }
}
