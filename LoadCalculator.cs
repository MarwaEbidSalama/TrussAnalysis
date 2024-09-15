using Autodesk.Revit.DB;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrussAnalysis.Models;

namespace TrussAnalysis
{
    public class LoadCalculator
    {
        public static double TotalDeadWeight { get; set; } 
        public static double TotalLiveWeight { get; set; }
        public static Vector<double> CalculateDeadLoad_Y(List<Element> elements)
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

            Vector<double> weights = Vector<double>.Build.Dense(new double[] { 0.5/9, 0.111111, 0.111111, 0.111111, 0.111111, 0.111111, 0.111111, 0.111111, 0.111111, 0.5/9 });
            
            double foronetruss = 0.5;

            TotalDeadWeight = totalDeadWeight;

            return totalDeadWeight* foronetruss * weights;
        }

        public static Vector<double> CalculateTrainLiveload_Y() {

            double axleLoad = 16000; // 16 tons but written in kg

            double impactForce_multipler = 1.3; // 30% increase in static load to account for dynamic load

            // weights are accounted for based on how the axle load moves to the nearest support.
            Vector<double> weights = Vector<double>.Build.Dense(new double[] { 1.5, 0.5, 0.5, 2, 1.5, 0, 1, 0, 1.5, 0.5 });

            TotalLiveWeight = axleLoad*weights.Sum();

            // already calculated for one truss bec. the bridge has two tracks.
            return axleLoad* impactForce_multipler * weights;
        }

        public static void AssignBrakingForce(List<Node> nodes)
        {
            double brakeForceCoefficient = 0.2;
            double load = brakeForceCoefficient * TotalLiveWeight;

            foreach (Node node in nodes)
            {
                if (node.ExternalLoadBearing)
                {
                    node.Fx_braking = load / 10;
                }
            }
        }

        public static void AssignLiveLoads(List<Node> nodes)
        {
            Vector<double> loads = CalculateTrainLiveload_Y();

            for (int i = 0; i < nodes.Where(x=>x.ExternalLoadBearing).Count(); i++)
            {
                nodes.Where(x => x.ExternalLoadBearing).ElementAt(i).Fy_live = -loads[i];
            }
        }

        public static void AssignDeadLoads(List<Node> nodes, List<Element> allElements)
        {

            Vector<double> loads = CalculateDeadLoad_Y(allElements);

            for (int i = 0; i < nodes.Where(x => x.ExternalLoadBearing).Count(); i++)
            {
                nodes.Where(x => x.ExternalLoadBearing).ElementAt(i).Fy_dead = -loads[i];
            }
        }
    }
}
