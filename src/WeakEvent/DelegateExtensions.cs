using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeakEvent
{
    public static class DelegateExtensions
    {
        public static TDelegate ConvertDelegate<TDelegate>(this Delegate d)
        {
            return (TDelegate)(Object)Delegate.CreateDelegate(typeof(TDelegate), d.Target, d.Method);
        }
    }
}

