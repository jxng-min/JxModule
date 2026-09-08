using System;
using System.Collections.Generic;
using System.Linq;

namespace JxModule.Terminal
{
    public sealed class JxTerminal
    {
        private readonly JxCommandRegistry _registry = new();

        public void Register(
            string key,
            CommandHandler handler,
            string description = null,
            IJxCommandCompletionProvider completionProvider = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Command key cannot be null or whitespace.", nameof(key));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Register(new JxCommandDescriptor(key.Trim(), handler, description, completionProvider));
        }

        public void Register(JxCommandDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (string.IsNullOrWhiteSpace(descriptor.Key))
            {
                throw new ArgumentException("Command key cannot be null or whitespace.", nameof(descriptor));
            }

            if (descriptor.Handler == null)
            {
                throw new ArgumentException("Command handler cannot be null.", nameof(descriptor));
            }

            _registry.Register(descriptor);
        }

        public bool Unregister(string key)
        {
            return _registry.Unregister(key);
        }

        public bool TryGetCommand(string key, out JxCommandDescriptor descriptor)
        {
            return _registry.TryGet(key, out descriptor);
        }

        public IEnumerable<string> GetCommandKeys()
        {
            return _registry.GetKeys();
        }

        public IEnumerable<JxCommandDescriptor> GetCommandDescriptors()
        {
            return _registry.GetDescriptors();
        }

        public JxCommandResult Execute(string input)
        {
            if (!JxCommandParser.TryTokenize(input, out var tokens))
            {
                return JxCommandResult.Failure("Invalid command");
            }

            if (!_registry.TryGetBestMatch(tokens, out var descriptor, out var argumentStartIndex))
            {
                return JxCommandResult.Failure($"Unknown command: {tokens[0]}");
            }

            var arguments = tokens.Skip(argumentStartIndex).ToArray();
            var context = new JxCommandContext(input, arguments);

            try
            {
                return descriptor.Handler.Invoke(context);
            }
            catch (Exception e)
            {
                return JxCommandResult.Failure(e.Message);
            }
        }

        public JxCompletionResult Complete(string input)
        {
            return Complete(input, input?.Length ?? 0);
        }

        public JxCompletionResult Complete(string input, int cursorPosition)
        {
            input ??= string.Empty;
            cursorPosition = Clamp(cursorPosition, 0, input.Length);

            var inputUntilCursor = input.Substring(0, cursorPosition);
            var currentTokenStartIndex = FindCurrentTokenStartIndex(inputUntilCursor);
            var currentToken = inputUntilCursor.Substring(currentTokenStartIndex);

            if (!JxCommandParser.TryTokenize(inputUntilCursor, out var tokens))
            {
                return CompleteCommandKey(string.Empty, 0, cursorPosition);
            }

            if (!_registry.TryGetBestMatch(tokens, out var descriptor, out var argumentStartIndex))
            {
                return CompleteCommandKey(inputUntilCursor, 0, cursorPosition);
            }

            if (tokens.Length <= argumentStartIndex && !EndsWithWhiteSpace(inputUntilCursor))
            {
                return CompleteCommandKey(inputUntilCursor, 0, cursorPosition);
            }

            if (descriptor.CompletionProvider == null)
            {
                return JxCompletionResult.Empty(cursorPosition);
            }

            var arguments = tokens.Skip(argumentStartIndex).ToArray();
            var currentArgumentIndex = Math.Max(0, arguments.Length - 1);
            if (EndsWithWhiteSpace(inputUntilCursor))
            {
                currentArgumentIndex = arguments.Length;
                currentToken = string.Empty;
                currentTokenStartIndex = cursorPosition;
            }

            var context = new JxCommandCompletionContext(
                input,
                descriptor.Key,
                arguments,
                currentArgumentIndex,
                currentToken,
                cursorPosition);

            var completedCandidates = descriptor.CompletionProvider.Complete(context) ?? Enumerable.Empty<string>();
            var candidates = completedCandidates
                .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
                .Where(candidate => candidate.StartsWith(currentToken, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return new JxCompletionResult(candidates, currentTokenStartIndex, cursorPosition - currentTokenStartIndex);
        }

        private JxCompletionResult CompleteCommandKey(string prefix, int replacementStartIndex, int replacementLength)
        {
            var candidates = _registry
                .GetKeys()
                .Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return new JxCompletionResult(candidates, replacementStartIndex, replacementLength);
        }

        private static int FindCurrentTokenStartIndex(string input)
        {
            for (var i = input.Length - 1; i >= 0; i--)
            {
                if (!char.IsWhiteSpace(input[i]))
                {
                    continue;
                }

                return i + 1;
            }

            return 0;
        }

        private static bool EndsWithWhiteSpace(string input)
        {
            return input.Length > 0 && char.IsWhiteSpace(input[^1]);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}
