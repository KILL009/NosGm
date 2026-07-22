using System.Collections.Generic;

namespace NosGm.PathFinder
{
    internal class MinHeap
    {
        #region Members

        private readonly List<Node> _array = new List<Node>();

        #endregion

        #region Properties

        public int Count => _array.Count;

        #endregion

        #region Methods

        public Node Pop()
        {
            var ret = _array[0];
            _array[0] = _array[_array.Count - 1];
            _array.RemoveAt(_array.Count - 1);

            var len = 0;
            while (len < _array.Count)
            {
                var min = len;
                if (2 * len + 1 < _array.Count && _array[2 * len + 1].CompareTo(_array[min]) == -1) min = 2 * len + 1;
                if (2 * len + 2 < _array.Count && _array[2 * len + 2].CompareTo(_array[min]) == -1) min = 2 * len + 2;

                if (min == len) break;
                var tmp = _array[len];
                _array[len] = _array[min];
                _array[min] = tmp;
                len = min;
            }

            return ret;
        }

        public void Push(Node element)
        {
            _array.Add(element);
            var len = _array.Count - 1;
            var parent = (len - 1) >> 1;
            while (len > 0 && _array[len].CompareTo(_array[parent]) < 0)
            {
                var tmp = _array[len];
                _array[len] = _array[parent];
                _array[parent] = tmp;
                len = parent;
                parent = (len - 1) >> 1;
            }
        }

        #endregion
    }
}