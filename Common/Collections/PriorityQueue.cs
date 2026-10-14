using System;
using System.Collections.Generic;

namespace JxModule.Collections.Generic
{
    public sealed class PriorityQueue<TElement, TPriority>
    {
        #region Entry Definition
        private readonly struct Entry
        {
            public readonly TElement element;
            public readonly TPriority priority;
            public readonly long sequence;

            public Entry(TElement element, TPriority priority, long sequence)
            {
                this.element = element;
                this.priority = priority;
                this.sequence = sequence;
            }
        }
        #endregion Entry Definition

        #region EntryComparer Definition
        private sealed class EntryComparer : IComparer<Entry>
        {
            private readonly IComparer<TPriority> _priorityComparer;

            public EntryComparer(IComparer<TPriority> priorityComparer)
            {
                _priorityComparer = priorityComparer;
            }

            public int Compare(Entry x, Entry y)
            {
                var result = _priorityComparer.Compare(x.priority, y.priority);
                return result != 0 ? result : x.sequence.CompareTo(y.sequence);
            }
        }
        #endregion EntryComparer Definition

        private readonly Heap<Entry> _heap;
        private long _sequence;

        public int Count => _heap.Count;

        public PriorityQueue(IComparer<TPriority> comparer = null, int capacity = 0)
        {
            var priorityComparer = comparer ?? Comparer<TPriority>.Default;
            _heap = new Heap<Entry>(new EntryComparer(priorityComparer), capacity);
        }

        public PriorityQueue(IEnumerable<(TElement Element, TPriority Priority)> items,
                             IComparer<TPriority> comparer = null)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var priorityComparer = comparer ?? Comparer<TPriority>.Default;
            var entries = CreateEntries(items);

            _heap = new Heap<Entry>(entries, new EntryComparer(priorityComparer));
        }

        public void Enqueue(TElement element, TPriority priority)
        {
            if (_sequence == long.MaxValue)
            {
                throw new InvalidOperationException("Priority queue sequence limit reached.");
            }

            _heap.Push(new Entry(element, priority, _sequence++));
        }

        public TElement Dequeue()
        {
            return _heap.Pop().element;
        }

        public bool TryDequeue(out TElement element, out TPriority priority)
        {
            if (!_heap.TryPop(out var entry))
            {
                element = default;
                priority = default;
                return false;
            }

            element = entry.element;
            priority = entry.priority;
            return true;
        }

        public TElement Peek()
        {
            return _heap.Peek().element;
        }

        public bool TryPeek(out TElement element, out TPriority priority)
        {
            if (!_heap.TryPeek(out var entry))
            {
                element = default;
                priority = default;
                return false;
            }

            element = entry.element;
            priority = entry.priority;
            return true;
        }

        public void Heapify(IEnumerable<(TElement Element, TPriority Priority)> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var entries = CreateEntries(items);

            _heap.Heapify(entries);
        }

        public void Clear()
        {
            _heap.Clear();
            _sequence = 0;
        }

        private List<Entry> CreateEntries(IEnumerable<(TElement Element, TPriority Priority)> items)
        {
            var entries = new List<Entry>();

            foreach (var item in items)
            {
                if (_sequence == long.MaxValue)
                {
                    throw new InvalidOperationException("Priority queue sequence limit reached.");
                }

                entries.Add(new Entry(item.Element, item.Priority, _sequence++));
            }

            return entries;
        }
    }
}
