using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Linq;
using TrussAnalysis.Models;

namespace TrussAnalysis
{
    internal class RevitUtils
    {
        public static void ShowResultsInRevit(UIDocument uidoc, Document document, TrussSystem truss)
        {
            double max = truss.Members.Select(x => x.StrainingAction).Max();
            double min = truss.Members.Select(x => x.StrainingAction).Min();

            RainbowColorMapper mapper = new RainbowColorMapper(min, max);

            ElementId solidFillPatternId = new FilteredElementCollector(document)
                     .OfClass(typeof(FillPatternElement))
                     .Cast<FillPatternElement>().FirstOrDefault(x => x.GetFillPattern().IsSolidFill).Id;

            View3D view1 = Create3DView(document, "Ultimate State Loading", true);

            using (Transaction t = new Transaction(document, "Show Axial Forces"))
            {
                t.Start();

                foreach (var member in truss.Members)
                {
                    Color color = mapper.GetColor(member.StrainingAction);
                    ApplySolidColorVisiblityOverrides(view1, member.Element, color, solidFillPatternId);

                    Parameter commentPara = member.Element.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                    if (commentPara!=null)
                    {
                        commentPara.Set($"Axial Force = {member.StrainingAction}");
                    }
                }

                t.Commit();
            }

            View3D view2 = Create3DView(document, "Safe Sections", true);

            using (Transaction t = new Transaction(document, "Show Safe Sections"))
            {
                t.Start();


                Color color = new Color(0, 0, 0);
                Color red = new Color(255, 0, 0);
                Color green = new Color(0, 255, 0);

                foreach (var member in truss.Members)
                {
                    double appliedStress = (Math.Abs(member.StrainingAction) / member.Area) * truss.FactorOfSafety;
                    if (appliedStress > member.YieldStress)
                    {
                        color = red;
                    }
                    else
                    {
                        color = green;
                    }

                    ApplySolidColorVisiblityOverrides(view2, member.Element, color, solidFillPatternId);
                }

                t.Commit();
            }

            uidoc.ActiveView = view1;
            uidoc.ActiveView = view2;


        }

        public static View3D Create3DView(Document document, string name, bool midDetailLevel = false)
        {
            View3D view3D = null;

            ElementId viewTypeId = new FilteredElementCollector(document).OfClass(typeof(ViewFamilyType))
                                  .WhereElementIsElementType()
                                  .Where(x => (x as ViewFamilyType).ViewFamily == ViewFamily.ThreeDimensional)
                                  .FirstOrDefault()?.Id;
            if (midDetailLevel)
            {
                viewTypeId = new FilteredElementCollector(document).OfClass(typeof(ViewFamilyType))
                     .WhereElementIsElementType()
                     .Where(x => (x as ViewFamilyType).ViewFamily == ViewFamily.ThreeDimensional
                     && ((x as ViewFamilyType).DefaultTemplateId == ElementId.InvalidElementId
                         || (x as ViewFamilyType).DefaultTemplateId == null
                         || (document.GetElement((x as ViewFamilyType).DefaultTemplateId) as View3D)?.ViewTemplateId == null
                         || (document.GetElement((x as ViewFamilyType).DefaultTemplateId) as View3D)?.ViewTemplateId == ElementId.InvalidElementId
                         || (document.GetElement((x as ViewFamilyType).DefaultTemplateId) as View3D)?.DetailLevel == ViewDetailLevel.Fine))
                     .FirstOrDefault()?.Id;
            }


            if (viewTypeId == null)
            {
                return view3D;
            }

            try
            {
                using (Transaction t = new Transaction(document, "Create 3D"))
                {
                    t.Start();

                    view3D = View3D.CreateIsometric(document, viewTypeId);

                    view3D.Name = name;
                    try
                    {
                        view3D.DetailLevel = ViewDetailLevel.Medium;
                        view3D.DisplayStyle = DisplayStyle.FlatColors;
                    }
                    catch (Exception)
                    {
                    }

                    t.Commit();
                }
            }
            catch (Exception ex)
            {
            }


            return view3D;
        }

        public static void ApplySolidColorVisiblityOverrides(View view, FamilyInstance element, Color color, ElementId solidFillPatternId)
        {
            OverrideGraphicSettings overrideGraphicSettings = new OverrideGraphicSettings();

            overrideGraphicSettings.SetCutBackgroundPatternColor(color);
            overrideGraphicSettings.SetCutForegroundPatternColor(color);
            overrideGraphicSettings.SetCutLineColor(color);

            overrideGraphicSettings.SetProjectionLineColor(new Color(0, 0, 0));
            overrideGraphicSettings.SetProjectionLineWeight(1);
            //overrideGraphicSettings.SetProjectionLineColor(color);

            overrideGraphicSettings.SetSurfaceBackgroundPatternColor(color);
            overrideGraphicSettings.SetSurfaceBackgroundPatternId(solidFillPatternId);
            overrideGraphicSettings.SetSurfaceForegroundPatternColor(color);
            overrideGraphicSettings.SetSurfaceForegroundPatternId(solidFillPatternId);

            overrideGraphicSettings.SetCutBackgroundPatternColor(color);

            view.SetElementOverrides(element.Id, overrideGraphicSettings);
        }
    }
}
