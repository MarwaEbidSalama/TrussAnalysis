using MathNet.Numerics.LinearAlgebra;
using System.Collections.Generic;
using System.Linq;

namespace TrussAnalysis.Models
{
    internal class TrussSystem
    {
        public double FactorOfSafety { get; }
        List<Member> Members { get; set; }
        List<Node> Nodes { get; set; }
        public int DOF { get; }

        public Matrix<double> StiffnessMatrix { get; set; }
        public Vector<double> ForceVector_Live { get; set; }
        public Vector<double> ForceVector_Dead { get; set; }
        public Vector<double> ForceVector_Braking { get; set; }
        public Vector<double> ForceVector_Combined { get; set; }
        public Vector<double> DisplacementVector { get; set; }
        public Vector<double> ForceVector_Final { get; set; }

        public bool[] KnownR { get; set; }
        public bool[] KnownU { get; set; }

        public TrussSystem(List<Member> members, List<Node> nodes)
        {
            this.FactorOfSafety = 1.25;

            this.Members = members;
            this.Nodes = nodes;
            this.DOF = nodes.Count() * 2;
            this.StiffnessMatrix = Matrix<double>.Build.Dense(DOF, DOF);

            this.ForceVector_Live = Vector<double>.Build.Dense(DOF);
            this.ForceVector_Dead = Vector<double>.Build.Dense(DOF);
            this.ForceVector_Braking = Vector<double>.Build.Dense(DOF);
            this.ForceVector_Combined = Vector<double>.Build.Dense(DOF);

            this.KnownR= new bool[DOF];
            this.KnownU = new bool[DOF];    

            this.DisplacementVector = Vector<double>.Build.Dense(DOF);

            LinkMembertoNode();

            CreateGlobalStiffnessMatrix();

            CreateForceVectors(1.35, 1.5, 1.5); // Ultimate Limit State (ULS)

            CreateBools();

            (DisplacementVector, ForceVector_Final) = StructuralEquationSolver.Solve(StiffnessMatrix, ForceVector_Combined, KnownR, KnownU, Vector<double>.Build.Dense(new double[] { 0,0,0,0 }));

            foreach (Node node in Nodes)
            {
                node.DOF1_Fx = ForceVector_Final[node.DOF1];
                node.DOF2_Fy = ForceVector_Final[node.DOF2];
                node.DOF1_Ux = DisplacementVector[node.DOF1];
                node.DOF2_Uy = DisplacementVector[node.DOF2];
            }

            foreach (Member member in Members)
            {
                Vector<double> U_Global = Vector<double>.Build.Dense(new double[] {
                    member.StartNode.DOF1_Ux,
                    member.StartNode.DOF2_Uy,
                    member.EndNode.DOF1_Ux,
                    member.EndNode.DOF2_Uy});

                member.U_local = member.TransformationMatrix * U_Global * member.TransformationMatrix.Transpose();
                member.N_local = member.k_local * member.U_local;
            }
        }

        private void CreateForceVectors(double scale_dead, double scale_live, double scale_braking)
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                ForceVector_Dead[Nodes[i].DOF2] = Nodes[i].Fy_dead;
                ForceVector_Live[Nodes[i].DOF2] = Nodes[i].Fy_live;
                ForceVector_Braking[Nodes[i].DOF1] = Nodes[i].Fx_braking;
            }

            ForceVector_Combined = scale_dead * ForceVector_Dead + scale_live * ForceVector_Live + scale_braking * ForceVector_Braking;
        }

        private void CreateBools()
        {
            foreach (Node node in Nodes)
            {
                if (node.HingeSupport)
                {
                    KnownU[node.DOF1] = true;
                    KnownU[node.DOF2] = true;
                    KnownR[node.DOF1] = false;
                    KnownR[node.DOF2] = false;
                }
                else
                {
                    KnownU[node.DOF1] = false;
                    KnownU[node.DOF2] = false;
                    KnownR[node.DOF1] = true;
                    KnownR[node.DOF2] = true;
                }
            }
        }

        private void CreateGlobalStiffnessMatrix()
        {
            foreach (var member in Members)
            {
                List<int> dofs = new List<int>() { member.StartNode.DOF1,
                                                  member.StartNode.DOF2,
                                                  member.EndNode.DOF1,
                                                  member.EndNode.DOF2};

                for (int i = 0; i < dofs.Count; i++)
                {
                    for (int j = 0; j < dofs.Count; j++)
                    {
                        int globalRow = dofs[i];
                        int globalCol = dofs[j];
                        StiffnessMatrix[globalRow, globalCol] += member.K_Global[i, j];
                    }
                }
            }
        }

        private void LinkMembertoNode()
        {
            foreach (Node node in Nodes)
            {
                IEnumerable<Member> associatedMembers = Members.Where(x => node.ConnectedMembers.Contains(x.Id));

                foreach (Member member in associatedMembers)
                {
                    member.AssignNodes(node);
                }
            }

            foreach (Member member in Members)
            {
                member.AssignMemberNameInSystem();
            }
        }
    }
}
