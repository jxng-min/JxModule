using System.Collections.Generic;
using UnityEngine;

namespace JxModule.Terminal
{
    public class JxTerminalHistory
    {
        private readonly List<string> _entries = new();
        private int _index;

        public void Push(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return;
            }

            if (_entries.Count == 0 || _entries[^1] != command)
            {
                _entries.Add(command);
            }

            _index = _entries.Count;
        }

        public string Previous()
        {
            if (_entries.Count == 0)
            {
                return null;
            }

            _index = Mathf.Max(0, _index - 1);
            return _entries[_index];
        }

        public string Next()
        {
            if (_entries.Count == 0)
            {
                return null;
            }

            _index = Mathf.Min(_entries.Count, _index + 1);
            
            return _index == _entries.Count ? string.Empty : _entries[_index];
        }
    }
}