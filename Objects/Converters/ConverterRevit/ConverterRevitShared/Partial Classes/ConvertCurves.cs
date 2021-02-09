using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Objects.BuiltElements.Revit.Curve;
using Speckle.Core.Models;
using System;
using System.Linq;

using DB = Autodesk.Revit.DB;
using DetailCurve = Objects.BuiltElements.Revit.Curve.DetailCurve;
using ModelCurve = Objects.BuiltElements.Revit.Curve.ModelCurve;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ModelCurve ModelCurveToSpeckle(DB.ModelCurve revitCurve)
    {
      var speckleCurve = new ModelCurve(CurveToSpeckle(revitCurve.GeometryCurve), revitCurve.LineStyle.Name);
      speckleCurve.elementId = revitCurve.Id.ToString();
      speckleCurve.applicationId = revitCurve.UniqueId;
      speckleCurve.units = ModelUnits;
      return speckleCurve;
    }

    public ApplicationPlaceholderObject ModelCurveToNative(ModelCurve speckleCurve)
    {
      var docObj = GetExistingElementByApplicationId(speckleCurve.applicationId);

      //TODO: support poliline/polycurve lines
      var baseCurve = CurveToNative(speckleCurve.baseCurve).get_Item(0);

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
      {
        revitCurve.LineStyle = Doc.GetElement(lineStyleId);
      }

      return new ApplicationPlaceholderObject() { applicationId = speckleCurve.applicationId, ApplicationGeneratedId = revitCurve.UniqueId, NativeObject = revitCurve };
    }

    // This is to support raw geometry being sent to Revit (eg from rhino, gh, autocad...)
    public ApplicationPlaceholderObject ModelCurveToNative(ICurve speckleLine)
    {
      // if it comes from GH it doesn't have an applicationId, the use the hash id
      if ((speckleLine as Base).applicationId == null)
        (speckleLine as Base).applicationId = (speckleLine as Base).id;

      var docObj = GetExistingElementByApplicationId((speckleLine as Base).applicationId);
      var baseCurve = CurveToNative(speckleLine).get_Item(0);

      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }

      DB.ModelCurve revitCurve = Doc.Create.NewModelCurve(baseCurve, NewSketchPlaneFromCurve(baseCurve));
      return new ApplicationPlaceholderObject() { applicationId = (speckleLine as Base).applicationId, ApplicationGeneratedId = revitCurve.UniqueId, NativeObject = revitCurve };
    }

    public DetailCurve DetailCurveToSpeckle(DB.DetailCurve revitCurve)
    {
      var speckleCurve = new DetailCurve(CurveToSpeckle(revitCurve.GeometryCurve), revitCurve.LineStyle.Name);
      speckleCurve.elementId = revitCurve.Id.ToString();
      speckleCurve.applicationId = revitCurve.UniqueId;
      speckleCurve.units = ModelUnits;
      return speckleCurve;
    }

    public ApplicationPlaceholderObject DetailCurveToNative(DetailCurve speckleCurve)
    {
      var docObj = GetExistingElementByApplicationId(speckleCurve.applicationId);

      //TODO: support polybline/polycurve lines
      var baseCurve = CurveToNative(speckleCurve.baseCurve).get_Item(0);

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
      catch (Exception)
      {
        ConversionErrors.Add(new Error("Detail curve creation failed", $"View is not valid for detail curve creation."));
        throw;
      }

      var lineStyles = revitCurve.GetLineStyleIds();
      var lineStyleId = lineStyles.FirstOrDefault(x => Doc.GetElement(x).Name == speckleCurve.lineStyle);
      if (lineStyleId != null)
      {
        revitCurve.LineStyle = Doc.GetElement(lineStyleId);
      }
      return new ApplicationPlaceholderObject() { applicationId = speckleCurve.applicationId, ApplicationGeneratedId = revitCurve.UniqueId, NativeObject = revitCurve };

    }

    public RoomBoundaryLine RoomBoundaryLineToSpeckle(DB.ModelCurve revitCurve)
    {
      var speckleCurve = new RoomBoundaryLine(CurveToSpeckle(revitCurve.GeometryCurve));
      speckleCurve.elementId = revitCurve.Id.ToString();
      speckleCurve.applicationId = revitCurve.UniqueId;
      speckleCurve.units = ModelUnits;
      return speckleCurve;
    }

    public ApplicationPlaceholderObject RoomBoundaryLineToNative(RoomBoundaryLine speckleCurve)
    {
      var docObj = GetExistingElementByApplicationId(speckleCurve.applicationId);

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
        var res = Doc.Create.NewRoomBoundaryLines(NewSketchPlaneFromCurve(baseCurve.get_Item(0)), baseCurve, Doc.ActiveView).get_Item(0);
        return new ApplicationPlaceholderObject() { applicationId = speckleCurve.applicationId, ApplicationGeneratedId = res.UniqueId, NativeObject = res };
      }
      catch (Exception)
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