using System.Collections.Generic;

namespace JxModule.Terminal
{
    public sealed class JxTerminalAutoComplete
    {
        private IReadOnlyList<string> _candidates;
        private int _candidateIndex;
        private int _replacementStartIndex;
        private int _replacementLength;
        private string _lastAppliedInput;
        private string _lastPreviewInput;

        public IReadOnlyList<string> Candidates => _candidates;
        public int CandidateIndex => _candidateIndex;
        public bool HasCandidates => _candidates != null && _candidates.Count > 0;

        public void Refresh(JxTerminal terminal, string input, int cursorPosition)
        {
            if (terminal == null)
            {
                Reset();
                return;
            }

            input ??= string.Empty;
            if (string.IsNullOrWhiteSpace(input))
            {
                Reset();
                return;
            }

            if (_lastPreviewInput == input)
            {
                return;
            }

            var result = terminal.Complete(input, cursorPosition);
            if (result == null || !result.HasCandidates)
            {
                Reset();
                _lastPreviewInput = input;
                return;
            }

            _candidates = result.Candidates;
            _candidateIndex = 0;
            _replacementStartIndex = result.ReplacementStartIndex;
            _replacementLength = result.ReplacementLength;
            _lastPreviewInput = input;
            _lastAppliedInput = null;
        }

        public string Complete(JxTerminal terminal,
                               string input,
                               int cursorPosition,
                               bool previous,
                               out int newCursorPosition)
        {
            newCursorPosition = cursorPosition;

            if (terminal == null)
            {
                Reset();
                return input;
            }

            input ??= string.Empty;

            if (!HasCandidates || input != _lastAppliedInput)
            {
                var result = terminal.Complete(input, cursorPosition);
                if (result == null || !result.HasCandidates)
                {
                    Reset();
                    return input;
                }

                _candidates = result.Candidates;
                _replacementStartIndex = result.ReplacementStartIndex;
                _replacementLength = result.ReplacementLength;
                _candidateIndex = previous ? _candidates.Count - 1 : 0;
                _lastPreviewInput = input;
            }
            else
            {
                _candidateIndex += previous ? -1 : 1;
                if (_candidateIndex < 0)
                {
                    _candidateIndex = _candidates.Count - 1;
                }
                else if (_candidateIndex >= _candidates.Count)
                {
                    _candidateIndex = 0;
                }
            }

            return ApplyCandidate(input, out newCursorPosition);
        }

        public void Reset()
        {
            _candidates = null;
            _candidateIndex = 0;
            _replacementStartIndex = 0;
            _replacementLength = 0;
            _lastAppliedInput = null;
            _lastPreviewInput = null;
        }

        private string ApplyCandidate(string input, out int newCursorPosition)
        {
            var candidate = _candidates[_candidateIndex];
            var removeLength = Clamp(_replacementLength, 0, input.Length - _replacementStartIndex);
            var completedInput = input
                .Remove(_replacementStartIndex, removeLength)
                .Insert(_replacementStartIndex, candidate);

            newCursorPosition = _replacementStartIndex + candidate.Length;
            _replacementLength = candidate.Length;
            _lastAppliedInput = completedInput;

            return completedInput;
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
