using Autodesk.Revit.DB;
using Objects.BuiltElements;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

using DB = Autodesk.Revit.DB;


namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {


    public List<ApplicationPlaceholderObject> GridLineToNative(GridLine speckleGridline)
    {
      var revitGrid = GetExistingElementByApplicationId(speckleGridline.applicationId) as Grid;
      if (revitGrid != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore)
        return new List<ApplicationPlaceholderObject>
      {
        new ApplicationPlaceholderObject
          {applicationId = speckleGridline.applicationId, ApplicationGeneratedId = revitGrid.UniqueId, NativeObject = revitGrid}
      }; ;

      var curve = CurveToNative(speckleGridline.baseLine).get_Item(0);

      //try update the gridline
      var isUpdate = false;
      if (revitGrid != null)
      {
        if (revitGrid.IsCurved)
        {
          Doc.Delete(revitGrid.Id); //not sure how to modify arc grids
        }
        else
        {
          //dim's magic from 1.0
          var oldStart = revitGrid.Curve.GetEndPoint(0);
          var oldEnd = revitGrid.Curve.GetEndPoint(1);

          var newStart = curve.GetEndPoint(0);
          var newEnd = curve.GetEndPoint(1);

          //only update if it has changed
          if (!(oldStart.DistanceTo(newStart) < TOLERANCE && oldEnd.DistanceTo(newEnd) < TOLERANCE))
          {
            var translate = newStart.Subtract(oldStart);
            ElementTransformUtils.MoveElement(Doc, revitGrid.Id, translate);

            var currentDirection = revitGrid.Curve.GetEndPoint(0).Subtract(revitGrid.Curve.GetEndPoint(1)).Normalize();
            var newDirection = newStart.Subtract(newEnd).Normalize();

            var angle = newDirection.AngleTo(currentDirection);

            if (angle > 0.00001)
            {
              var crossProd = newDirection.CrossProduct(currentDirection).Z;
              ElementTransformUtils.RotateElement(Doc, revitGrid.Id, Autodesk.Revit.DB.Line.CreateUnbound(newStart, XYZ.BasisZ), crossProd < 0 ? angle : -angle);
            }

            try
            {
              var datumLine = revitGrid.GetCurvesInView(DatumExtentType.Model, Doc.ActiveView)[0];
              var datumLineZ = datumLine.GetEndPoint(0).Z;
              //note the new datum line has endpoints flipped!
              revitGrid.SetCurveInView(DatumExtentType.Model, Doc.ActiveView, Line.CreateBound(new XYZ(newEnd.X, newEnd.Y, datumLineZ), new XYZ(newStart.X, newStart.Y, datumLineZ)));
            }
            catch (Exception e)
            {
              Report.LogConversionError(new Exception($"Error setting grid endpoints {speckleGridline.id}."));
            }
            isUpdate = true;
          }
        }
      }

      //create the grid
      if (revitGrid == null)
      {
        if (curve is Arc a)
          revitGrid = Grid.Create(Doc, a);
        else if (curve is Line l)
          revitGrid = Grid.Create(Doc, l);
        else
          throw new Speckle.Core.Logging.SpeckleException("Failed to create GridLine, curve type not supported for Grid: " + curve.GetType().FullName);
      }

      if (!string.IsNullOrEmpty(speckleGridline.label))
      {
        var names = new FilteredElementCollector(Doc).WhereElementIsElementType().OfClass(typeof(Grid)).ToElements().Cast<Grid>().ToList().Select(x => x.Name);
        if (!names.Contains(speckleGridline.label))
          revitGrid.Name = speckleGridline.label;
      }

      var placeholders = new List<ApplicationPlaceholderObject>()
      {
        new ApplicationPlaceholderObject
        {
        applicationId = speckleGridline.applicationId,
        ApplicationGeneratedId = revitGrid.UniqueId,
        NativeObject = revitGrid
        }
      };

      Report.Log($"{(isUpdate ? "Updated" : "Created")} GridLine {revitGrid.Id}");
      return placeholders;
    }

    public GridLine GridLineToSpeckle(DB.Grid revitGridLine)
    {
      var speckleGridline = new GridLine();
      speckleGridline.baseLine = CurveToSpeckle(revitGridLine.Curve);
      speckleGridline.label = revitGridLine.Name;

      //speckleGridline.elementId = revitCurve.Id.ToString(); this would need a RevitGridLine element
      speckleGridline.applicationId = revitGridLine.UniqueId;
      speckleGridline.units = ModelUnits;
      Report.Log($"Converted GridLine {revitGridLine.Id}");
      return speckleGridline;
    }

  }
}