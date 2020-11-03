using Autodesk.Revit.DB;
using Objects;
using Objects.Geometry;
using Objects.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DB = Autodesk.Revit.DB;
using DetailCurve = Objects.Revit.DetailCurve;
using ModelCurve = Objects.Revit.ModelCurve;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ModelCurve ModelCurveToSpeckle(DB.ModelCurve revitCurve)
    {
      var speckleCurve = new ModelCurve();
      speckleCurve.baseCurve = CurveToSpeckle(revitCurve.GeometryCurve);
      speckleCurve.lineStyle = revitCurve.LineStyle.Name;

      return speckleCurve;

    }

    public DB.ModelCurve ModelCurveToNative(ModelCurve speckleCurve)
    {
      var (docObj, stateObj) = GetExistingElementByApplicationId(speckleCurve.applicationId, "");

      //TODO: support poliline/polycurve lines
      var baseCurve = CurveToNative(speckleCurve.baseCurve as ICurve).get_Item(0);

      //delete and re-create line
      //TODO: check if can be modified
      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }

      DB.ModelCurve revitCurve = Doc.Create.NewModelCurve(baseCurve, NewSketchPlaneFromCurve(baseCurve));

      var lineStyles = revitCurve.GetLineStyleIds();
      var lineStyleId = lineStyles.FirstOrDefault(x => Doc.GetElement(x).Name == speckleCurve.lineStyle);
      if (lineStyleId != null)
        revitCurve.LineStyle = Doc.GetElement(lineStyleId) ;
      return revitCurve;

    }

    public DetailCurve DetailCurveToSpeckle(DB.DetailCurve revitCurve)
    {
      var speckleCurve = new DetailCurve();
      speckleCurve.baseCurve = CurveToSpeckle(revitCurve.GeometryCurve);
      speckleCurve.lineStyle = revitCurve.LineStyle.Name;

      return speckleCurve;

    }

    public DB.DetailCurve DetailCurveToNative(DetailCurve speckleCurve)
    {
      var (docObj, stateObj) = GetExistingElementByApplicationId(speckleCurve.applicationId, "");

      //TODO: support polybline/polycurve lines
      var baseCurve = CurveToNative(speckleCurve.baseCurve as ICurve).get_Item(0);

      //delete and re-create line
      //TODO: check if can be modified
      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }
      DB.DetailCurve revitCurve = null;
      try
      {
        revitCurve = Doc.Create.NewDetailCurve(Doc.ActiveView, baseCurve);
      }
      catch (Exception e)
      {
        ConversionErrors.Add(new Error ( "Detail curve creation failed", $"View is not valid for detail curve creation." ));
        throw;
      }

      var lineStyles = revitCurve.GetLineStyleIds();
      var lineStyleId = lineStyles.FirstOrDefault(x => Doc.GetElement(x).Name == speckleCurve.lineStyle);
      if (lineStyleId != null)
        revitCurve.LineStyle = Doc.GetElement(lineStyleId);
      return revitCurve;

    }

    public RoomBoundaryLine RoomBoundaryLineToSpeckle(DB.ModelCurve revitCurve)
    {
      var speckleCurve = new RoomBoundaryLine();
      speckleCurve.baseCurve = CurveToSpeckle(revitCurve.GeometryCurve);

      return speckleCurve;

    }

    public DB.ModelCurve RoomBoundaryLineToNative(RoomBoundaryLine speckleCurve)
    {
      var (docObj, stateObj) = GetExistingElementByApplicationId(speckleCurve.applicationId, "");

      //TODO: support poliline/polycurve lines
      var baseCurve = CurveToNative(speckleCurve.baseCurve as ICurve);

      //delete and re-create line
      //TODO: check if can be modified
      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }

      try
      {
        return Doc.Create.NewRoomBoundaryLines(NewSketchPlaneFromCurve(baseCurve.get_Item(0)), baseCurve, Doc.ActiveView).get_Item(0);
      }
      catch (Exception e)
      {
        ConversionErrors.Add(new Error("Room boundary line creation failed", $"View is not valid for room boundary line creation."));
        throw;
      }


    }


    /// <summary>
    /// Credits: Grevit
    /// Creates a new Sketch Plane from a Curve
    /// https://github.com/grevit-dev/Grevit/blob/3c7a5cc198e00dfa4cc1e892edba7c7afd1a3f84/Grevit.Revit/Utilities.cs#L402
    /// </summary>
    /// <param name="curve">Curve to get plane from</param>
    /// <returns>Plane of the curve</returns>
    private SketchPlane NewSketchPlaneFromCurve(DB.Curve curve)
    {
      XYZ startPoint = curve.GetEndPoint(0);
      XYZ endPoint = curve.GetEndPoint(1);

      // If Start end Endpoint are the same check further points.
      int i = 2;
      while (startPoint == endPoint && endPoint != null)
      {
        endPoint = curve.GetEndPoint(i);
        i++;
      }

      // Plane to return
      DB.Plane plane;

      // If Z Values are equal the Plane is XY
      if (startPoint.Z == endPoint.Z)
      {
        plane = CreatePlane(XYZ.BasisZ, startPoint);
      }
      // If X Values are equal the Plane is YZ
      else if (startPoint.X == endPoint.X)
      {
        plane = CreatePlane(XYZ.BasisX, startPoint);
      }
      // If Y Values are equal the Plane is XZ
      else if (startPoint.Y == endPoint.Y)
      {
        plane = CreatePlane(XYZ.BasisY, startPoint);
      }
      // Otherwise the Planes Normal Vector is not X,Y or Z.
      // We draw lines from the Origin to each Point and use the Plane this one spans up.
      else
      {
        CurveArray curves = new CurveArray();
        curves.Append(curve);
        curves.Append(DB.Line.CreateBound(new XYZ(0, 0, 0), startPoint));
        curves.Append(DB.Line.CreateBound(endPoint, new XYZ(0, 0, 0)));

        plane = DB.Plane.CreateByThreePoints(startPoint, new XYZ(0, 0, 0), endPoint);
      }

      return SketchPlane.Create(Doc, plane);
    }

    private DB.Plane CreatePlane(XYZ basis, XYZ startPoint)
    {
      return DB.Plane.CreateByNormalAndOrigin(basis, startPoint);
    }


  }
}
