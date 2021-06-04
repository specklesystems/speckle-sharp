using Autodesk.Revit.DB;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Objects.BuiltElements.Revit.Curve;
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
      var curve = CurveToNative(speckleGridline.baseLine).get_Item(0);

      //delete and re-create line
      //TODO: check if can be modified
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
            revitGrid.SetCurveInView(DatumExtentType.Model, Doc.ActiveView, Line.CreateBound(newStart, newEnd));
          }
          catch (Exception e)
          {
            System.Diagnostics.Debug.WriteLine("Failed to set grid endpoints.");
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
          throw new Speckle.Core.Logging.SpeckleException("Curve type not supported for Grid: " + curve.GetType().FullName);
      }

      //name must be unique, too much faff
      //revitGrid.Name = speckleGridline.label;

      var placeholders = new List<ApplicationPlaceholderObject>()
      {
        new ApplicationPlaceholderObject
        {
        applicationId = speckleGridline.applicationId,
        ApplicationGeneratedId = revitGrid.UniqueId,
        NativeObject = revitGrid
        }
      };


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
      return speckleGridline;
    }

  }
}