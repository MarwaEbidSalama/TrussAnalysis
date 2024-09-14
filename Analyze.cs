using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrussAnalysis.Models;
using static Autodesk.Revit.DB.SpecTypeId;

namespace TrussAnalysis
{
    [Transaction(TransactionMode.Manual)]
    internal class Analyze : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = commandData.Application.ActiveUIDocument.Document;

            Truss truss = SelectTruss(uidoc) as Truss;

            List<StructuralConnectionHandler> loadbearing_connections = SelectConnections(uidoc, "load bearing");
            List<StructuralConnectionHandler> connections = SelectConnections(uidoc, "rest of the");

            List<Member> members = Member.ProcessMembers(truss);
            List<Node> nodes = Node.ProcessNodes(loadbearing_connections, true);
            nodes.AddRange(Node.ProcessNodes(connections, false));




            return Result.Succeeded;
        }

        private Element SelectTruss(UIDocument uidoc)
        {
            try
            {
                Autodesk.Revit.DB.Reference reference = uidoc.Selection.PickObject(ObjectType.Element,new TrussSelection(), "Please Select the Truss for Analysis");
                return uidoc.Document.GetElement(reference.ElementId);
            }
            catch (Exception)
            {
                return null;    
            }
        }

        private List<StructuralConnectionHandler> SelectConnections(UIDocument uidoc, string extra)
        {
            try
            {
                List<Autodesk.Revit.DB.Reference> references = uidoc.Selection.PickObjects(ObjectType.Element, new ConnectionSelection(), $"Please Select the {extra} Truss Connections")?.ToList();
                List<StructuralConnectionHandler> connectors = references.Select(x=>uidoc.Document.GetElement(x.ElementId) as StructuralConnectionHandler).ToList();
                return connectors;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    internal class TrussSelection : ISelectionFilter
    {
        public bool AllowReference(Autodesk.Revit.DB.Reference reference, XYZ position)
        {
            return false;
        }

        bool ISelectionFilter.AllowElement(Element elem)
        {
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralTruss)
            {
                return true;    
            }

            return false;
        }
    }

    internal class ConnectionSelection : ISelectionFilter
    {
        public bool AllowReference(Autodesk.Revit.DB.Reference reference, XYZ position)
        {
            return false;
        }

        bool ISelectionFilter.AllowElement(Element elem)
        {
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructConnections)
            {
                return true;
            }

            return false;
        }

    }
}
