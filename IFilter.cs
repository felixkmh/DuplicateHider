using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuplicateHider
{
    public abstract class IFilter<Input, Output>
    {
        public abstract Output ApplySingle(in Input input);
    }

    public abstract class IFilter<T> : IFilter<T, T>
    {
        public static IFilter<T> MakeChain(params IFilter<T>[] filters)
        {
            if (filters.Length == 0) return null;
            for(int i = 1; i < filters.Length; ++i)
            {
                filters[i - 1].NextFilter = filters[i];
            }
            return filters[0];
        }

        public IFilter<T> SetNext(IFilter<T> next)
        {
            NextFilter = next;
            return next;
        }

        public IFilter<T> NextFilter { get; protected set; } = null;

        public T Apply(in T input)
        {
            T result = ApplySingle(input);
            if (NextFilter != null)
                return NextFilter.Apply(result);
            else
                return result;
        }
    }

    public static class FilterExtensions
    {
        public static T Filter<T, Filter>(this T input,  Filter filter) where Filter : IFilter<T>
        {
            return filter.Apply(input);
        }
    }
}
