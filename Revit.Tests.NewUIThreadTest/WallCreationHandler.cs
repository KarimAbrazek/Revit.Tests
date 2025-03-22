using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Document = Autodesk.Revit.DB.Document;


namespace Revit.Tests.NewUIThreadTest;
public class WallCreationHandler : IExternalEventHandler
{
    public int TotalWalls { get; set; } = 50;
    public int CurrentWall { get; set; } = 0;
    public bool IsCancelled { get; set; } = false;
    public Action<int, int> ProgressCallback { get; set; }

    public void Execute(UIApplication app)
    {
        Autodesk.Revit.DB.Document doc = app.ActiveUIDocument.Document;

        try
        {
            CurrentWall = 0;
            IsCancelled = false;

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Create Multiple Walls");

                for (int i = 0; i < TotalWalls; i++)
                {
                    if (IsCancelled)
                    {
                        tx.RollBack();
                        return;
                    }
                    CreateWall(doc, i);
                    CurrentWall = i + 1;
                    ProgressCallback?.Invoke(CurrentWall, TotalWalls);
                }

                tx.Commit();
            }
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Error", $"Failed to create walls: {ex.Message}");
        }
    }

    private void CreateWall(Document doc, int index)
    {
        double x = 0;
        double y = index * 10;
        Line curve = Line.CreateBound(new XYZ(x, y, 0), new XYZ(x + 20, y, 0));

        FilteredElementCollector collector = new FilteredElementCollector(doc);
        collector.OfClass(typeof(WallType));
        WallType wallType = collector.FirstElement() as WallType;
        Wall.Create(doc, curve, wallType.Id,
                    doc.ActiveView.GenLevel.Id,
                    10, 0, false, false);
    }

    public string GetName()
    {
        return "Wall Creation Handler";
    }
}
