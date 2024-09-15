using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrussAnalysis
{
    internal class StructuralEquationSolver
    {
        public static (Vector<double>, Vector<double>) Solve(Matrix<double> K, Vector<double> F, bool[] knownF, bool[] knownU, Vector<double> KnownU_Values)
        {
            int n = K.RowCount;
            bool[] unknownF = knownF.Select(k => !k).ToArray();
            bool[] unknownU = knownU.Select(k => !k).ToArray();

            // Get indices of known and unknown values
            int[] unknownIndices = Enumerable.Range(0, n).Where(i => unknownU[i]).ToArray();
            int[] knownIndices = Enumerable.Range(0, n).Where(i => knownU[i]).ToArray();

            int unknownCount = unknownIndices.Length;
            int knownCount = knownIndices.Length;

            // Rearrange K, F, and U
            Matrix<double> Kuu = ExtractSubMatrix(K, unknownIndices, unknownIndices);
            Matrix<double> Kuk = ExtractSubMatrix(K, unknownIndices, knownIndices);
            Matrix<double> Kku = ExtractSubMatrix(K, knownIndices, unknownIndices);
            Matrix<double> Kkk = ExtractSubMatrix(K, knownIndices, knownIndices);

            Vector<double> Fu = ExtractSubVector(F, unknownIndices);
            Vector<double> Fk = ExtractSubVector(F, knownIndices);
            Vector<double> ForcesOnSupports = Vector<double>.Build.Dense(new double[] { 0, 0, 0, 0 });

            Vector<double> Uk = Vector<double>.Build.Dense(knownCount);
            for (int i = 0; i < knownCount; i++)
            {
                Uk[i] = KnownU_Values[i];
                ForcesOnSupports[i] = F[knownIndices[i]]; 
            }


            // Solve for unknown displacements
            Vector<double> Uu = Kuu.Solve(Fu - Kuk * Uk);

            // Calculate reactions
            Vector<double> R = Kku * Uu + Kkk * Uk - ForcesOnSupports;

            // Combine results
            Vector<double> U = Vector<double>.Build.Dense(n);
            Vector<double> combinedF = Vector<double>.Build.Dense(n);

            for (int i = 0; i < n; i++)
            {
                if (unknownU[i])
                    U[i] = Uu[Array.IndexOf(unknownIndices, i)];
                else
                    U[i] = Uk[Array.IndexOf(knownIndices, i)];

                if (knownF[i])
                    combinedF[i] = F[i];
                else
                    combinedF[i] = R[Array.IndexOf(knownIndices, i)];
            }

            Console.WriteLine("Displacements U:");
            Console.WriteLine(U);
            Console.WriteLine("\nForces F (including reactions):");
            Console.WriteLine(combinedF);

            return (U, combinedF);
        }

        private static Matrix<double> ExtractSubMatrix(Matrix<double> matrix, int[] rowIndices, int[] colIndices)
        {
            Matrix<double> subMatrix = Matrix<double>.Build.Dense(rowIndices.Length, colIndices.Length);
            for (int i = 0; i < rowIndices.Length; i++)
            {
                for (int j = 0; j < colIndices.Length; j++)
                {
                    subMatrix[i, j] = matrix[rowIndices[i], colIndices[j]];
                }
            }
            return subMatrix;
        }

        private static Vector<double> ExtractSubVector(Vector<double> vector, int[] indices)
        {
            return Vector<double>.Build.Dense(indices.Select(i => vector[i]).ToArray());
        }

        public static Vector<double> Solve2(Matrix<double> K, Vector<double> F, bool[] knownU, bool[] hasReaction)
        {
            int n = K.RowCount;
            bool[] unknownU = knownU.Select(k => !k).ToArray();

            // Get indices of known and unknown values
            int[] unknownIndices = Enumerable.Range(0, n).Where(i => unknownU[i]).ToArray();
            int[] knownIndices = Enumerable.Range(0, n).Where(i => knownU[i]).ToArray();
            int[] reactionIndices = Enumerable.Range(0, n).Where(i => hasReaction[i]).ToArray();

            int unknownCount = unknownIndices.Length;
            int knownCount = knownIndices.Length;
            int reactionCount = reactionIndices.Length;

            // Rearrange K, F, and U
            Matrix<double> Kuu = ExtractSubMatrix(K, unknownIndices, unknownIndices);
            Matrix<double> Kuk = ExtractSubMatrix(K, unknownIndices, knownIndices);
            Matrix<double> Kur = ExtractSubMatrix(K, unknownIndices, reactionIndices);
            Matrix<double> Kru = ExtractSubMatrix(K, reactionIndices, unknownIndices);
            Matrix<double> Krk = ExtractSubMatrix(K, reactionIndices, knownIndices);

            Vector<double> Fu = ExtractSubVector(F, unknownIndices);
            Vector<double> Fk = ExtractSubVector(F, knownIndices);
            Vector<double> Fr = ExtractSubVector(F, reactionIndices);

            Vector<double> Uk = Vector<double>.Build.Dense(knownCount);
            for (int i = 0; i < knownCount; i++)
                Uk[i] = F[knownIndices[i]]; // Known displacements are stored in F

            // Solve for unknown displacements and reactions
            Matrix<double> combinedK = Matrix<double>.Build.Dense(unknownCount + reactionCount, unknownCount + reactionCount);
            combinedK.SetSubMatrix(0, 0, Kuu);
            combinedK.SetSubMatrix(0, unknownCount, Kur);
            combinedK.SetSubMatrix(unknownCount, 0, Kru);

            Vector<double> combinedF = Vector<double>.Build.Dense(unknownCount + reactionCount);
            combinedF.SetSubVector(0,(Fu - Kuk * Uk).Count, Fu - Kuk * Uk);
            combinedF.SetSubVector(unknownCount, (Fr - Krk * Uk).Count,Fr - Krk * Uk);

            Vector<double> solution = combinedK.Solve(combinedF);

            Vector<double> Uu = solution.SubVector(0, unknownCount);
            Vector<double> R = solution.SubVector(unknownCount, reactionCount);

            // Combine results
            Vector<double> U = Vector<double>.Build.Dense(n);
            Vector<double> combinedForces = Vector<double>.Build.Dense(n);

            for (int i = 0; i < n; i++)
            {
                if (unknownU[i])
                    U[i] = Uu[Array.IndexOf(unknownIndices, i)];
                else
                    U[i] = Uk[Array.IndexOf(knownIndices, i)];

                if (hasReaction[i])
                    combinedForces[i] = F[i] + R[Array.IndexOf(reactionIndices, i)];
                else
                    combinedForces[i] = F[i];
            }

            Console.WriteLine("Displacements U:");
            Console.WriteLine(U);
            Console.WriteLine("\nForces F (including reactions):");
            Console.WriteLine(combinedForces);

            return U;
        }

        public static void Main()
        {
            // Example usage
            Matrix<double> K = Matrix<double>.Build.DenseOfArray(new double[,]
            {
            { 2, -1, 0 },
            { -1, 2, -1 },
            { 0, -1, 1 }
            });

            Vector<double> F = Vector<double>.Build.Dense(new double[] { 0, 10, 0 });
            bool[] knownF = new bool[] { true, false, true };  // Reactions at 1st and 3rd DOF
            bool[] knownU = new bool[] { false, false, true }; // Fixed displacement at 3rd DOF

            // Set known displacement (e.g., 0 at the 3rd DOF)
            F[2] = 0; // Use F to store known displacement

        }
    }
}
