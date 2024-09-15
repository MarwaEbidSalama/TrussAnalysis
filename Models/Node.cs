using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrussAnalysis.Models
{
    public class Node
    {
        public XYZ Location { get; set; }
        List<ElementId> ConnectedMembers { get; set; }
        public ElementId Id { get; set; }
        public double Fx { get; set; }
        public double Fy { get; set; }

        public int Order { get; set; }
        public bool ExternalLoadBearing { get; set; }

        public static List<Node> ProcessNodes(List<StructuralConnectionHandler> connectors, bool loadBearing)
        {
            List<Node> nodes = connectors.Select(x=> new Node(x,loadBearing)).ToList();
            return nodes;
        }

        public Node(StructuralConnectionHandler connector, bool loadBearing)
        {
            ConnectedMembers = connector.GetConnectedElementIds().ToList();
            Id = connector.Id;
            Location = connector.get_BoundingBox(connector.Document.ActiveView).Max;
            ExternalLoadBearing = loadBearing;
            Fx = 0;
            Fy = 0;

            string mark = connector.get_Parameter(BuiltInParameter.ALL_MODEL_MARK).AsValueString();
            if (string.IsNullOrEmpty(mark))
            {
                int.TryParse(mark, out int mark_int);
                Order = mark_int;
            }
        }

    }
}
