using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuplicateHider.Cache
{
    interface IGeneratorCache<TItem, TArg>
    {
        TItem GetOrGenerate(TArg arg);
        void Clear();
    }
}
