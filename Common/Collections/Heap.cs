using System;
using System.Collections.Generic;

namespace JxModule.Collections.Generic
{
    public sealed class Heap<T>
    {
        private readonly List<T> _items;
        private readonly IComparer<T> _comparer;

        public int Count => _items.Count;

        public Heap(IComparer<T> comparer = null, int capacity = 0)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _comparer = comparer ?? Comparer<T>.Default;
            _items = new List<T>(capacity);
        }
        
        public Heap(IEnumerable<T> items, IComparer<T> comparer = null)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            _comparer = comparer ?? Comparer<T>.Default;
            _items = new List<T>(items);

            Heapify();
        }
        
        public void Heapify(IEnumerable<T> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            var snapshot = new List<T>(items);

            _items.Clear();
            _items.AddRange(snapshot);

            Heapify();
        }
        
        public void Heapify()
        {
            for (var i = _items.Count / 2 - 1; i >= 0; i--)
            {
                SiftDown(i);
            }
        }

        public void Push(T item)
        {
            _items.Add(item);
            SiftUp(_items.Count - 1);
        }

        public T Pop()
        {
            if (_items.Count == 0)
            {
                throw new InvalidOperationException("Heap is empty.");
            }

            var root = _items[0];
            var lastIndex = _items.Count - 1;

            if (lastIndex == 0)
            {
                _items.RemoveAt(0);
                return root;
            }

            _items[0] = _items[lastIndex];
            _items.RemoveAt(lastIndex);

            SiftDown(0);

            return root;
        }

        public T Peek()
        {
            if (_items.Count == 0)
            {
                throw new InvalidOperationException("Heap is empty.");
            }

            return _items[0];
        }

        public bool TryPop(out T item)
        {
            if (_items.Count == 0)
            {
                item = default;
                return false;
            }

            item = Pop();
            return true;
        }

        public bool TryPeek(out T item)
        {
            if (_items.Count == 0)
            {
                item = default;
                return false;
            }

            item = _items[0];
            return true;
        }

        public void Clear()
        {
            _items.Clear();
        }

        private void SiftUp(int index)
        {
            var item = _items[index];

            while (index > 0)
            {
                var parent = (index - 1) / 2;

                if (_comparer.Compare(item, _items[parent]) >= 0)
                    break;

                _items[index] = _items[parent];
                index = parent;
            }

            _items[index] = item;
        }

        private void SiftDown(int index)
        {
            var count = _items.Count;
            var item = _items[index];

            while (true)
            {
                var left = index * 2 + 1;

                if (left >= count)
                    break;

                var right = left + 1;
                var child = left;

                if (right < count &&
                    _comparer.Compare(_items[right], _items[left]) < 0)
                {
                    child = right;
                }

                if (_comparer.Compare(item, _items[child]) <= 0)
                    break;

                _items[index] = _items[child];
                index = child;
            }

            _items[index] = item;
        }
    }
}
