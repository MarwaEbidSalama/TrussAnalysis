using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrussAnalysis.Models
{
    public class Member
    {
        public double YoungModulus { get; set; }
        public double YieldStress { get; private set; }
        public double Length { get; set; }
        public double Area { get; set; }
        public double k_member { get; set; }
        public Matrix<double> k_local { get; set; }
        public Matrix<double> K_Global { get; set; }
        public Vector<double> U_local { get; set; }
        public Vector<double> N_local { get; set; }
        public XYZ direction { get; set; }
        public double AngleBetTrussLocationLine { get; set; }
        public Matrix<double> TransformationMatrix { get; set; }
        public double q { get; set; }
        public Node StartNode { get; set; }
        public Node EndNode { get; set; }
        public Line LocationLine { get; set; }
        public FamilyInstance Element { get; set; }
        public ElementId Id { get; set; }

        private Document doc;
        public string StructuralUsage { get; set; }
        public string Name { get; private set; }

        public static List<Member> ProcessMembers(Truss truss)
        {
            List<FamilyInstance> revit_members = truss.Members.Select(x => truss.Document.GetElement(x) as FamilyInstance).ToList();

            XYZ trussDirection = ((truss.Location as LocationCurve).Curve as Line).Direction;

            List<Member> members = revit_members.Select(x => new Member(x, trussDirection)).ToList();

            return members;
        }

        public Member(FamilyInstance instance, XYZ trussDirection)
        {
            this.Id = instance.Id;
            this.Element = instance;
            this.doc = instance.Document;
            this.StructuralUsage = instance.StructuralUsage.ToString();

            this.YoungModulus = (doc.GetElement((doc.GetElement(instance.StructuralMaterialId) as Material).StructuralAssetId) as PropertySetElement)
                                .GetStructuralAsset().YoungModulus.X;
            this.YieldStress = (doc.GetElement((doc.GetElement(instance.StructuralMaterialId) as Material).StructuralAssetId) as PropertySetElement)
                    .GetStructuralAsset().MinimumYieldStress;

            this.LocationLine = (instance.Location as LocationCurve).Curve as Line;
            this.direction = LocationLine.Direction;
            this.Length = LocationLine.Length;

            Parameter areaParam1 = instance.Symbol.LookupParameter("A");
            Parameter areaParam2 = instance.Symbol.LookupParameter("Section Area");
            if (areaParam1 != null)
            {
                this.Area = areaParam1.AsDouble();
            }
            else if (areaParam2 != null)
            {
                this.Area = areaParam2.AsDouble();
            }

            k_member = (YoungModulus * Area) / Length;

            AngleBetTrussLocationLine = direction.AngleTo(trussDirection);

            CreateTransformationMatrix();

            CreateStiffnessMatrix_Locally();

            CreateStiffnessMatrix_Globally();

        }

        private void CreateTransformationMatrix()
        {
            double c = Math.Cos(AngleBetTrussLocationLine);
            double s = Math.Sin(AngleBetTrussLocationLine);

            this.TransformationMatrix = Matrix<double>.Build.DenseOfArray(new double[,] {
                        {  c,  s,  0,  0 },
                        { -s,  c,  0,  0 },
                        {  0,  0,  c,  s },
                        {  0,  0, -s,  c },
                    });
        }

        private void CreateStiffnessMatrix_Globally()
        {
            this.K_Global = TransformationMatrix.Transpose() * k_local * TransformationMatrix;

            for (int i = 0; i < K_Global.RowCount; i++)
            {
                for (int j = 0; j < K_Global.ColumnCount; j++)
                {
                    this.K_Global[i,j] = MathUtils.RoundToNearest((long)this.K_Global[i, j], 1000);
                }
            }
        }

        private void CreateStiffnessMatrix_Locally()
        {
            Matrix<double> m = Matrix<double>.Build.DenseOfArray(new double[,] {
                        {  1,  0, -1,  0 },
                        {  0,  0,  0,  0 },
                        { -1,  0,  1,  0 },
                        {  0,  0,  0,  0 },
                    });

            this.k_local = m * k_member;
        }

        public void AssignNodes(Node node)
        {
            if (StartNode == null || EndNode == null)
            {
                List<XYZ> endpoints = LocationLine.Tessellate().ToList().OrderBy(x => x.X).ToList();
                XYZComparer comparer = new XYZComparer();

                if (comparer.Equals(node.Location, endpoints[0]))
                {
                    this.StartNode = node;
                }
                else if (comparer.Equals(node.Location, endpoints[1]))
                {
                    this.EndNode = node;
                }
            }
        }

        public void AssignMemberNameInSystem()
        {
            this.Name = StartNode.Name + "-" + EndNode.Name;
        }

    }
}
