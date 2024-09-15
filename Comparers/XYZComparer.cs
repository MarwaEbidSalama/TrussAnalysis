using Autodesk.Revit.DB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrussAnalysis
{
    internal class XYZComparer : IEqualityComparer<XYZ>
    {
        private DoubleComparer doubleComparer;
        public XYZComparer()
        {
           doubleComparer = new DoubleComparer(3.5);
        }

        public new bool Equals(XYZ x, XYZ y)
        {
            if (doubleComparer.Equals(x.X,y.X)
                &&
                doubleComparer.Equals(x.Y, y.Y)
                &&
                doubleComparer.Equals(x.Z, y.Z))
            {
                return true;
            }

            return false;
        }

        public int GetHashCode(XYZ obj)
        {
            return 0;
        }
    }
}
