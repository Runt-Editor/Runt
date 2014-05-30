using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runt.Service
{
    public class Utils
    {
        public static Func<T, T> Update<T>(Func<T, T> update)
            where T : class
        {
            return value => value == null ? null : update(value);
        }
    }
}
