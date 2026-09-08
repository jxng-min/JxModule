namespace JxModule.Terminal
{
    public sealed class JxCommandDescriptor
    {
        public string Key { get; }
        public CommandHandler Handler { get; }
        public string Description { get; }
        public IJxCommandCompletionProvider CompletionProvider { get; }

        public JxCommandDescriptor(string key,
                                   CommandHandler handler,
                                   string description = null,
                                   IJxCommandCompletionProvider completionProvider = null)
        {
            Key = key;
            Handler = handler;
            Description = description;
            CompletionProvider = completionProvider;
        }
    }
}
