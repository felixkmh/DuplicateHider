using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace DuplicateHider
{
    class Cache<T> : Stack<T>
    {
        public bool HasItems { get => Count > 0; }

        public int Capacity {
            get => capacity;
            set 
            {
                capacity = Math.Max(0, value);
                while (Count > capacity)
                    Pop();
                TrimExcess();
            }
        }
        protected int capacity;

        public new void Push(T item)
        {
            if (Count < capacity)
                base.Push(item);
        }


        public Cache(int capacity) : base(capacity)
        {
            this.capacity = capacity;
        }
    }
}
