using System;
using System.Collections.Generic;

namespace NosGm.PathFinder
{
    public class BinaryHeap<T> where T : IComparable<T>
    {
        private T[] _items;
        public int Count { get; private set; }

        public BinaryHeap(int capacity = 128)
        {
            _items = new T[capacity];
        }

        public void Push(T item)
        {
            if (Count == _items.Length)
            {
                Array.Resize(ref _items, _items.Length * 2);
            }

            _items[Count] = item;
            SiftUp(Count);
            Count++;
        }

        public T Pop()
        {
            if (Count == 0) throw new InvalidOperationException("Heap is empty");

            T root = _items[0];
            Count--;
            _items[0] = _items[Count];
            SiftDown(0);
            return root;
        }

        public void Clear()
        {
            Count = 0;
        }

        private void SiftUp(int index)
        {
            int parent = (index - 1) / 2;
            while (index > 0 && _items[index].CompareTo(_items[parent]) < 0)
            {
                T temp = _items[index];
                _items[index] = _items[parent];
                _items[parent] = temp;
                index = parent;
                parent = (index - 1) / 2;
            }
        }

        private void SiftDown(int index)
        {
            while (true)
            {
                int left = 2 * index + 1;
                int right = 2 * index + 2;
                int smallest = index;

                if (left < Count && _items[left].CompareTo(_items[smallest]) < 0)
                    smallest = left;

                if (right < Count && _items[right].CompareTo(_items[smallest]) < 0)
                    smallest = right;

                if (smallest == index) break;

                T temp = _items[index];
                _items[index] = _items[smallest];
                _items[smallest] = temp;
                index = smallest;
            }
        }
    }
}
