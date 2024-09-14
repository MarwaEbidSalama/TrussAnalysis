using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrussAnalysis
{
    public class LoadCalculator
    {
        public static double CalculateDeadLoad(List<Element> elements)
        {
            double totalDeadWeight = 0;
            double density = 0;
            double volume = 0;

            foreach (Element element in elements)
            {
                Document doc = element.Document;
                if (element is Floor floor)
                {
                    density = (doc.GetElement((doc.GetElement(floor.FloorType.StructuralMaterialId) as Material).StructuralAssetId) as PropertySetElement)
                               .GetStructuralAsset().Density;

                    volume = floor.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED).AsDouble();
                }
                else if (element is FamilyInstance instance)
                {
                    density = (doc.GetElement((doc.GetElement(instance.StructuralMaterialId) as Material).StructuralAssetId) as PropertySetElement)
                               .GetStructuralAsset().Density;

                    volume = instance.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED).AsDouble();
                }

                totalDeadWeight += density * volume;
            }

            return totalDeadWeight;
        }
    }
}
