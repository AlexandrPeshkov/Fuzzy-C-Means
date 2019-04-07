using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fuzzy_C_Means
{
    public static class Helpers
    {
        public static List<double> DivideBy(this List<double> left, List<double> right)
        {
            List<double> result = new List<double>();
            for(var i=0; i<left.Count; i++)
            {
                result.Add(left[i] / right[i]);
            }
            return result;
        }
    }
}
