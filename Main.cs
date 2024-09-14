using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace TrussAnalysis
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalApplication
    {

        UIControlledApplication application;
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            this.application = application;
            CreateRibbon();
            return Result.Succeeded;
        }

        private void CreateRibbon()
        {
            try
            {
                application.CreateRibbonTab("Truss Analysis");
            }
            catch
            { }
            RibbonPanel panel = application.CreateRibbonPanel("Truss Analysis", "Preliminary FEM");

            PushButtonData analyze = new PushButtonData("Analyze", "Analyze", this.GetType().Assembly.Location, typeof(TrussAnalysis.Analyze).FullName);
            analyze.LargeImage = ImageUtils.ConvertBitmapToBitmapImage(TrussAnalysis.Properties.Resources.bridge32);
            analyze.Image = ImageUtils.ConvertBitmapToBitmapImage(TrussAnalysis.Properties.Resources.bridge16);

            panel.AddItem(analyze);
        }
    }
}
