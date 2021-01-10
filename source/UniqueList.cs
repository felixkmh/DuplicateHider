using System.Collections;
using System.Collections.Generic;

namespace DuplicateHider
{
    public class UniqueList<T> : IList<T>
    {
        internal List<T> data;

        public UniqueList()
        {
            data = new List<T>();
        }

        public UniqueList(IEnumerable<T> collection)
        {
            data = new List<T>(collection);
        }

        public UniqueList(int capacity)
        {
            data = new List<T>(capacity);
        }

        public T this[int index] { get => data[index]; set => data[index] = value; }

        public int Count => data.Count;

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            if (!data.Contains(item))
            {
                data.Add(item);
            }
        }

        public void Clear()
        {
            data.Clear();
        }

        public bool Contains(T item)
        {
            return data.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            data.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return data.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return data.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            data.Insert(index, item);
        }

        public bool Remove(T item)
        {
            return data.Remove(item);
        }

        public void RemoveAt(int index)
        {
            data.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return data.GetEnumerator();
        }
    }
}
