using System;
using System.Collections.Generic;

namespace JxModule.Terminal
{
    public sealed class JxCompletionResult
    {
        public IReadOnlyList<string> Candidates { get; }
        public int ReplacementStartIndex { get; }
        public int ReplacementLength { get; }
        public bool HasCandidates => Candidates.Count > 0;

        public JxCompletionResult(IReadOnlyList<string> candidates,
                                  int replacementStartIndex,
                                  int replacementLength)
        {
            Candidates = candidates ?? Array.Empty<string>();
            ReplacementStartIndex = replacementStartIndex;
            ReplacementLength = replacementLength;
        }

        public static JxCompletionResult Empty(int cursorPosition)
        {
            return new JxCompletionResult(Array.Empty<string>(), cursorPosition, 0);
        }
    }
}
