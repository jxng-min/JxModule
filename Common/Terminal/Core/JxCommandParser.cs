using System;

namespace JxModule.Terminal
{
    public static class JxCommandParser
    {
        public static bool TryTokenize(string input, out string[] tokens)
        {
            tokens = Array.Empty<string>();

            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return tokens.Length > 0;
        }

        public static bool TryParse(string input, out string key, out string[] arguments)
        {
            key = null;
            arguments = Array.Empty<string>();

            if (!TryTokenize(input, out var tokens))
            {
                return false;
            }
            
            key = tokens[0];
            if (tokens.Length == 1)
            {
                return true;
            }
            
            arguments = new string[tokens.Length - 1];
            Array.Copy(tokens, 1, arguments, 0, arguments.Length);
            
            return true;
        }
    }
}
