using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrussAnalysis
{
    public class RainbowColorMapper
    {
        private List<double> values;
        private double minValue;
        private double maxValue;

        public RainbowColorMapper(double minValue, double maxValue)
        {
            this.minValue = minValue;
            this.maxValue = maxValue;
        }

        public Color GetColor(double value)
        {
            // Normalize the value to a range of 0 to 1
            double normalizedValue = (value - minValue) / (maxValue - minValue);

            // Define color stops for the rainbow
            Color[] colorStops = new Color[]
            {
            new Autodesk.Revit.DB.Color(255, 0, 0),    // red
            new Autodesk.Revit.DB.Color(255, 165, 0),  // orange
            new Autodesk.Revit.DB.Color(255, 255, 0),  // yellow
            new Autodesk.Revit.DB.Color(0, 128, 0),    // green
            new Autodesk.Revit.DB.Color(0, 255, 255),  // cyan
            new Autodesk.Revit.DB.Color(0, 0, 255),    // blue
            };


            // Find the appropriate color segment
            int segmentCount = colorStops.Length - 1;
            double segmentLength = 1.0 / segmentCount;
            int segment = (int)(normalizedValue * segmentCount);
            segment = Math.Min(segment, segmentCount - 1);

            // Calculate the position within the segment
            double segmentPosition = (normalizedValue - (segment * segmentLength)) / segmentLength;

            // Interpolate between the two colors in the segment
            Color color1 = colorStops[segment];
            Color color2 = colorStops[segment + 1];

            int r = (int)(color1.Red + (color2.Red - color1.Red) * segmentPosition);
            int g = (int)(color1.Green + (color2.Green - color1.Green) * segmentPosition);
            int b = (int)(color1.Blue + (color2.Blue - color1.Blue) * segmentPosition);

            return new Color(Byte.Parse(r.ToString()), Byte.Parse(g.ToString()), Byte.Parse(b.ToString()));
        }
    }
}
