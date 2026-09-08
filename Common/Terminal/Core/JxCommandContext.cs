using System.Collections.Generic;

namespace JxModule.Terminal
{
    public sealed class JxCommandContext
    {
        public string RawInput { get; }
        public IReadOnlyList<string> Arguments { get; }

        public JxCommandContext(string rawInput, IReadOnlyList<string> arguments)
        {
            RawInput = rawInput;
            Arguments = arguments;
        }
    }
    
    public delegate JxCommandResult CommandHandler(JxCommandContext context);
}