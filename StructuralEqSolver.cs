using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrussAnalysis
{
    public class StructuralEqSolver
    {
        public class SystemEquation
        {
            public Vector<double> F { get; set; }  // Force vector
            public Matrix<double> K { get; set; }  // Stiffness matrix
            public bool[] KnownF { get; set; }     // Boolean array indicating known force components
        }

        public class SolutionResult
        {
            public Vector<double> Displacement { get; set; }
            public Vector<double> Force { get; set; }
        }

        public static SolutionResult Solve(SystemEquation equation)
        {
            int n = equation.K.RowCount;

            // Validate input
            if (equation.F == null || equation.F.Count != n)
                throw new ArgumentException("Force vector F must have the same length as the system size");
            if (equation.KnownF == null || equation.KnownF.Length != n)
                throw new ArgumentException("KnownF array must have the same length as the system size");

            var knownFIndices = new System.Collections.Generic.List<int>();
            var unknownFIndices = new System.Collections.Generic.List<int>();

            // Separate known F and unknown F indices
            for (int i = 0; i < n; i++)
            {
                if (equation.KnownF[i])
                    knownFIndices.Add(i);
                else
                    unknownFIndices.Add(i);
            }

            // Create matrices for the linear system Ax = b
            int unknownDispCount = knownFIndices.Count;
            var A = Matrix<double>.Build.Dense(unknownDispCount, unknownDispCount);
            var b = Vector<double>.Build.Dense(unknownDispCount);

            for (int i = 0; i < unknownDispCount; i++)
            {
                int row = knownFIndices[i];
                for (int j = 0; j < unknownDispCount; j++)
                {
                    int col = knownFIndices[j];
                    A[i, j] = equation.K[row, col];
                }
                b[i] = equation.F[row];
            }

            // Solve for unknown displacements
            var unknownDisplacements = A.Solve(b);

            // Construct the full displacement vector
            var finalDisplacement = Vector<double>.Build.Dense(n);
            for (int i = 0; i < unknownDispCount; i++)
            {
                finalDisplacement[knownFIndices[i]] = unknownDisplacements[i];
            }

            // Calculate forces
            var finalForce = equation.K * finalDisplacement;

            // Ensure known forces are preserved
            for (int i = 0; i < knownFIndices.Count; i++)
            {
                int index = knownFIndices[i];
                finalForce[index] = equation.F[index];
            }

            return new SolutionResult
            {
                Displacement = finalDisplacement,
                Force = finalForce
            };
        }
    }
}
