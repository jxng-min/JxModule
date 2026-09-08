using System.Collections.Generic;
using System.Linq;

namespace JxModule.Terminal
{
    public sealed class JxCommandRegistry
    {
        private readonly Dictionary<string, JxCommandDescriptor> _commands = new();

        public void Register(JxCommandDescriptor descriptor)
        {
            _commands[descriptor.Key] = descriptor;
        }

        public bool Unregister(string key)
        {
            return _commands.Remove(key);
        }

        public bool TryGet(string key, out JxCommandDescriptor descriptor)
        {
            return _commands.TryGetValue(key, out descriptor);
        }

        public bool TryGetBestMatch(
            IReadOnlyList<string> tokens,
            out JxCommandDescriptor descriptor,
            out int argumentStartIndex)
        {
            descriptor = null;
            argumentStartIndex = 0;

            for (var length = tokens.Count; length > 0; length--)
            {
                var key = string.Join(" ", tokens.Take(length));
                if (!_commands.TryGetValue(key, out descriptor))
                {
                    continue;
                }

                argumentStartIndex = length;
                return true;
            }

            return false;
        }

        public IEnumerable<string> GetKeys()
        {
            return _commands.Keys;
        }

        public IEnumerable<JxCommandDescriptor> GetDescriptors()
        {
            return _commands.Values;
        }
    }
}
