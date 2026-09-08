using System.Collections.Generic;

namespace JxModule.Terminal
{
    public sealed class JxCommandCompletionContext
    {
        public string RawInput { get; }
        public string CommandKey { get; }
        public IReadOnlyList<string> Arguments { get; }
        public int CurrentArgumentIndex { get; }
        public string CurrentToken { get; }
        public int CursorPosition { get; }

        public JxCommandCompletionContext(string rawInput,
                                          string commandKey,
                                          IReadOnlyList<string> arguments,
                                          int currentArgumentIndex,
                                          string currentToken,
                                          int cursorPosition)
        {
            RawInput = rawInput;
            CommandKey = commandKey;
            Arguments = arguments;
            CurrentArgumentIndex = currentArgumentIndex;
            CurrentToken = currentToken;
            CursorPosition = cursorPosition;
        }
    }
}
