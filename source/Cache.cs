using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Controls;

namespace DuplicateHider
{
    class UnboundedCache<T> : Stack<T>
    {
        public bool HasItems { get => Count > 0; }

        protected readonly Func<T> generate;

        public virtual new void Push(T item)
        {
            base.Push(item);
        }

        public virtual void Consume<Collection>(Collection collection)
            where Collection : IList, ICollection, IEnumerable
        {
            for (int i = 0; i < collection.Count; ++i)
            {
                if (collection[i] is T item)
                {
                    base.Push(item);
                }
            }
            collection.Clear();
        }

        internal int Recycled = 0;
        internal int Generated = 0;

        public readonly Func<T> Get;

        internal T GetOrGenerate()
        {
            T item = default(T);
            if (Count > 0)
            {
                ++Recycled;
                item = Pop();
            }
            else if (generate != null)
            {
                ++Generated;
                item = generate.Invoke();
            }
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Recycled={Recycled}, Generated={Generated}, Count={Count}"); 
#endif
            return item;
        }

        public UnboundedCache(Func<T> generator = null) : base()
        {
            this.generate = generator;
            if (generate != null)
            {
                Get = GetOrGenerate;
            }
            else
            {
                Get = () =>
                {
                    if (Count > 0)
                        return Pop();
                    return default(T);
                };
            }
        }
    }


    class BoundedCache<T> : UnboundedCache<T>
    {
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

        public override void Push(T item)
        {
            if (Count < capacity)
            {
                base.Push(item);
            }
        }

        public override void Consume<Collection>(Collection collection) 
        {
            for (int i = 0; i + Count < capacity && i < collection.Count; ++i)
            {
                if (collection[i] is T item)
                {
                    base.Push(item);
                }
            }
            collection.Clear();
        }

        public BoundedCache(int capacity, Func<T> generator = null, bool prefill = false) : base(generator)
        {
            this.capacity = capacity;
            if (generate != null && prefill)
            {
                for (int i = 0; i < capacity; ++i)
                {
                    base.Push(generate());
                }
            }
        }
    }
}
