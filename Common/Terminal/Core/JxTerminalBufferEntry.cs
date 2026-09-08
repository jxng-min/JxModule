namespace JxModule.Terminal
{
    public readonly struct JxTerminalBufferEntry
    {
        public EJxTerminalBufferEntryType Type { get; }
        public string Message { get; }

        public JxTerminalBufferEntry(EJxTerminalBufferEntryType type, string message)
        {
            Type = type;
            Message = message;
        }
    }
}
