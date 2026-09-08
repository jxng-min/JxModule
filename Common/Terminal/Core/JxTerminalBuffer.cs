using System.Collections.Generic;

namespace JxModule.Terminal
{
    public sealed class JxTerminalBuffer
    {
        private readonly List<JxTerminalBufferEntry> _entries = new();

        public IReadOnlyList<JxTerminalBufferEntry> Entries => _entries;

        public void Append(EJxTerminalBufferEntryType type, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            var lines = message.Replace("\r\n", "\n").Split('\n');
            foreach (var line in lines)
            {
                _entries.Add(new JxTerminalBufferEntry(type, line));
            }
        }

        public void AppendInput(string message)
        {
            Append(EJxTerminalBufferEntryType.Input, message);
        }

        public void AppendOutput(string message)
        {
            Append(EJxTerminalBufferEntryType.Output, message);
        }

        public void AppendSuccess(string message)
        {
            Append(EJxTerminalBufferEntryType.Success, message);
        }

        public void AppendWarning(string message)
        {
            Append(EJxTerminalBufferEntryType.Warning, message);
        }

        public void AppendError(string message)
        {
            Append(EJxTerminalBufferEntryType.Error, message);
        }

        public void AppendResult(JxCommandResult result)
        {
            if (string.IsNullOrEmpty(result.Message))
            {
                return;
            }

            Append(result.IsSuccess ? EJxTerminalBufferEntryType.Success : EJxTerminalBufferEntryType.Error, result.Message);
        }

        public void Clear()
        {
            _entries.Clear();
        }
    }
}
