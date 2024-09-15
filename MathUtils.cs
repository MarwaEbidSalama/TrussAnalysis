using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrussAnalysis
{
    public  class MathUtils
    {
        public static long RoundToNearest(long value, long nearest)
        {
            return (long)(Math.Round((double)value / nearest) * nearest);
        }
    }
}
