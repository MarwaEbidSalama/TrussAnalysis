using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrussAnalysis.Models
{
    public class Node
    {
        public XYZ Location { get; set; }
        public List<ElementId> ConnectedMembers { get; set; }
        public ElementId Id { get; set; }

        // External Force On node
        public double Fx_braking { get; set; }
        public double Fy_dead { get; set; }
        public double Fy_live { get; set; }

        public string Name { get; set; }

        public int DOF1 { get; set; }
        public int DOF2 { get; set; }

        public double DOF1_Ux { get; set; }
        public double DOF2_Uy { get; set; }
        public double DOF1_Fx { get; set; }
        public double DOF2_Fy { get; set; }

        public bool ExternalLoadBearing { get; set; }
        public bool HingeSupport { get; set; }


        public static List<Node> ProcessNodes(List<StructuralConnectionHandler> connectors, List<Element> allElements, List<Member> members)
        {
            List<Node> nodes = connectors.Select(x=> new Node(x)).ToList();

            List<Node> nodes_ordered = nodes.OrderBy(x => x.Name).ToList();


            AssignDOFIndices(nodes_ordered);
            LoadCalculator.AssignDeadLoads(nodes_ordered, allElements);
            LoadCalculator.AssignLiveLoads(nodes_ordered);
            LoadCalculator.AssignBrakingForce(nodes_ordered);

            return nodes_ordered;
        }

        public static void AssignDOFIndices(List<Node> nodes)
        {
            int counter = -1;
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].DOF1 = counter + 1;
                nodes[i].DOF2 = nodes[i].DOF1 + 1;

                counter+=2;
            }
        }

        public Node(StructuralConnectionHandler connector)
        {
            ConnectedMembers = connector.GetConnectedElementIds().ToList();
            Id = connector.Id;
            Location = connector.get_BoundingBox(connector.Document.ActiveView).Max;

            string mark = connector.get_Parameter(BuiltInParameter.ALL_MODEL_MARK).AsValueString();
            if (!string.IsNullOrEmpty(mark))
            {
                Name = mark;
            }

            string comment = connector.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString();
            if (!string.IsNullOrEmpty(comment) && comment == "loaded")
            {
                this.ExternalLoadBearing = true;
            }

            if (Name=="a" || Name == "j")
            {
                this.HingeSupport = true;
            }
        }

    }
}
