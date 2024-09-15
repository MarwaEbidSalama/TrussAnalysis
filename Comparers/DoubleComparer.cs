using System;
using System.Collections.Generic;

namespace TrussAnalysis
{
    internal class DoubleComparer : IEqualityComparer<double>
    {
        double tolerance {  get; set; }

        public DoubleComparer( double tol)
        {
                this.tolerance = tol;
        }

        public bool Equals(double x, double y)
        {
            if (Math.Abs(Math.Abs(x) - Math.Abs(y)) < tolerance)
            {
                return true;
            }

            return false;
        }

        public int GetHashCode(double obj)
        {
            return 0;
        }
    }
}
