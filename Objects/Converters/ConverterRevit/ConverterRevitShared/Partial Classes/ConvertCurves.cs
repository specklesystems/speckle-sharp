using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit.Curve;
using Speckle.Core.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Alignment = Objects.BuiltElements.Alignment;
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
      Report.Log($"Converted ModelCurve {revitCurve.Id}");
      return speckleCurve;
    }

    public List<ApplicationPlaceholderObject> AlignmentToNative(Alignment alignment)
    {
      var docObj = GetExistingElementByApplicationId(alignment.applicationId);
      if (docObj != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore)
        return new List<ApplicationPlaceholderObject>
      {
        new ApplicationPlaceholderObject
          {applicationId = alignment.applicationId, ApplicationGeneratedId = docObj.UniqueId, NativeObject = docObj}
      };

      //delete and re-create line
      //TODO: check if can be modified
      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }

      var curves = CurveToNative(alignment.curves);
      var placeholders = new List<ApplicationPlaceholderObject>();
      var curveEnumerator = curves.GetEnumerator();
      while (curveEnumerator.MoveNext() && curveEnumerator.Current != null)
      {
        var baseCurve = curveEnumerator.Current as DB.Curve;
        DB.ModelCurve revitCurve = Doc.Create.NewModelCurve(baseCurve, NewSketchPlaneFromCurve(baseCurve, Doc));
        var lineStyles = revitCurve.GetLineStyleIds().First();
        placeholders.Add(new ApplicationPlaceholderObject() { applicationId = alignment.applicationId, ApplicationGeneratedId = revitCurve.UniqueId, NativeObject = revitCurve });
        Report.Log($"Created Alignment {revitCurve.Id}");
      }

      return placeholders;
    }

    public List<ApplicationPlaceholderObject> ModelCurveToNative(ModelCurve speckleCurve)
    {
      var docObj = GetExistingElementByApplicationId(speckleCurve.applicationId);
      if (docObj != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore)
        return new List<ApplicationPlaceholderObject>
      {
        new ApplicationPlaceholderObject
          {applicationId = speckleCurve.applicationId, ApplicationGeneratedId = docObj.UniqueId, NativeObject = docObj}
      };
      //delete and re-create line
      //TODO: check if can be modified
      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }

      var curves = CurveToNative(speckleCurve.baseCurve);
      var placeholders = new List<ApplicationPlaceholderObject>();
      var curveEnumerator = curves.GetEnumerator();
      while (curveEnumerator.MoveNext() && curveEnumerator.Current != null)
      {
        var baseCurve = curveEnumerator.Current as DB.Curve;
        DB.ModelCurve revitCurve = Doc.Create.NewModelCurve(baseCurve, NewSketchPlaneFromCurve(baseCurve, Doc));

        var lineStyles = revitCurve.GetLineStyleIds();
        var lineStyleId = lineStyles.FirstOrDefault(x => Doc.GetElement(x).Name == speckleCurve.lineStyle);
        if (lineStyleId != null)
        {
          revitCurve.LineStyle = Doc.GetElement(lineStyleId);
        }
        placeholders.Add(new ApplicationPlaceholderObject() { applicationId = speckleCurve.applicationId, ApplicationGeneratedId = revitCurve.UniqueId, NativeObject = revitCurve });
        Report.Log($"Created ModelCurve {revitCurve.Id}");
      }

      return placeholders;
    }

    // This is to support raw geometry being sent to Revit (eg from rhino, gh, autocad...)
    public List<ApplicationPlaceholderObject> ModelCurveToNative(ICurve speckleLine)
    {
      // if it comes from GH it doesn't have an applicationId, the use the hash id
      if ((speckleLine as Base).applicationId == null)
        (speckleLine as Base).applicationId = (speckleLine as Base).id;

      var docObj = GetExistingElementByApplicationId((speckleLine as Base).applicationId);
      if (docObj != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore)
        return new List<ApplicationPlaceholderObject>
      {
        new ApplicationPlaceholderObject
          {applicationId = (speckleLine as Base).applicationId, ApplicationGeneratedId = docObj.UniqueId, NativeObject = docObj}
      };

      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }

      try
      {
        return ModelCurvesFromEnumerator(CurveToNative(speckleLine).GetEnumerator(), speckleLine);
      }
      catch (Exception e)
      {
        if (e.Message.Contains("lies outside")) // this happens if curves are very far from origin
        {
          Report.LogConversionError(e); return null;
        }
        // use display value if curve fails (prob a closed, periodic curve or a non-planar nurbs)
        if (speckleLine is IDisplayValue<Geometry.Polyline> d)
          return ModelCurvesFromEnumerator(CurveToNative(d.displayValue).GetEnumerator(),
            speckleLine);
        else
          return new List<ApplicationPlaceholderObject>();
      }
    }

    public List<ApplicationPlaceholderObject> ModelCurvesFromEnumerator(IEnumerator curveEnum, ICurve speckleLine)
    {
      var placeholders = new List<ApplicationPlaceholderObject>();
      while (curveEnum.MoveNext() && curveEnum.Current != null)
      {
        var curve = curveEnum.Current as DB.Curve;
        // Curves must be bound in order to be valid model curves
        if (!curve.IsBound) curve.MakeBound(speckleLine.domain.start ?? 0, speckleLine.domain.end ?? Math.PI * 2);
        DB.ModelCurve revitCurve = null;

        if (Doc.IsFamilyDocument)
        {
          revitCurve = Doc.FamilyCreate.NewModelCurve(curve, NewSketchPlaneFromCurve(curve, Doc));
        }
        else
        {
          revitCurve = Doc.Create.NewModelCurve(curve, NewSketchPlaneFromCurve(curve, Doc));
        }

        Report.Log($"Created ModelCurve {revitCurve.Id}");
        placeholders.Add(new ApplicationPlaceholderObject()
        {
          applicationId = (speckleLine as Base).applicationId,
          ApplicationGeneratedId = revitCurve.UniqueId,
          NativeObject = revitCurve
        });
      }

      return placeholders;
    }

    public DetailCurve DetailCurveToSpeckle(DB.DetailCurve revitCurve)
    {
      var speckleCurve = new DetailCurve(CurveToSpeckle(revitCurve.GeometryCurve), revitCurve.LineStyle.Name);
      speckleCurve.elementId = revitCurve.Id.ToString();
      speckleCurve.applicationId = revitCurve.UniqueId;
      speckleCurve.units = ModelUnits;
      Report.Log($"Converted DetailCurve {revitCurve.Id}");
      return speckleCurve;
    }

    public List<ApplicationPlaceholderObject> DetailCurveToNative(DetailCurve speckleCurve)
    {
      var docObj = GetExistingElementByApplicationId(speckleCurve.applicationId);
      if (docObj != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore)
        return new List<ApplicationPlaceholderObject>
      {
        new ApplicationPlaceholderObject
          {applicationId = speckleCurve.applicationId, ApplicationGeneratedId = docObj.UniqueId, NativeObject = docObj}
      }; ;
      //delete and re-create line
      //TODO: check if can be modified
      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }

      var placeholders = new List<ApplicationPlaceholderObject>();
      var crvEnum = CurveToNative(speckleCurve.baseCurve).GetEnumerator();
      while (crvEnum.MoveNext() && crvEnum.Current != null)
      {
        var baseCurve = crvEnum.Current as DB.Curve;
        DB.DetailCurve revitCurve = null;
        try
        {
          revitCurve = Doc.Create.NewDetailCurve(Doc.ActiveView, baseCurve);
        }
        catch (Exception)
        {
          throw (new Exception($"Detail curve creation failed\nView is not valid for detail curve creation."));
        }

        var lineStyles = revitCurve.GetLineStyleIds();
        var lineStyleId = lineStyles.FirstOrDefault(x => Doc.GetElement(x).Name == speckleCurve.lineStyle);
        if (lineStyleId != null)
        {
          revitCurve.LineStyle = Doc.GetElement(lineStyleId);
        }
        placeholders.Add(new ApplicationPlaceholderObject() { applicationId = speckleCurve.applicationId, ApplicationGeneratedId = revitCurve.UniqueId, NativeObject = revitCurve });
        Report.Log($"Created DetailCurve {revitCurve.Id}");
      }

      return placeholders;

    }

    public RoomBoundaryLine RoomBoundaryLineToSpeckle(DB.ModelCurve revitCurve)
    {
      var speckleCurve = new RoomBoundaryLine(CurveToSpeckle(revitCurve.GeometryCurve));
      speckleCurve.elementId = revitCurve.Id.ToString();
      speckleCurve.applicationId = revitCurve.UniqueId;
      speckleCurve.units = ModelUnits;
      Report.Log($"Converted RoomBoundaryLine {revitCurve.Id}");
      return speckleCurve;
    }

    public ApplicationPlaceholderObject RoomBoundaryLineToNative(RoomBoundaryLine speckleCurve)
    {
      var docObj = GetExistingElementByApplicationId(speckleCurve.applicationId);
      if (docObj != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore)
        return new ApplicationPlaceholderObject
        { applicationId = speckleCurve.applicationId, ApplicationGeneratedId = docObj.UniqueId, NativeObject = docObj };
      var baseCurve = CurveToNative(speckleCurve.baseCurve);

      //delete and re-create line
      //TODO: check if can be modified
      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }

      try
      {
        var revitCurve = Doc.Create.NewRoomBoundaryLines(NewSketchPlaneFromCurve(baseCurve.get_Item(0), Doc), baseCurve, Doc.ActiveView).get_Item(0);
        Report.Log($"Created RoomBoundaryLine {revitCurve.Id}");
        return new ApplicationPlaceholderObject()
        { applicationId = speckleCurve.applicationId, ApplicationGeneratedId = revitCurve.UniqueId, NativeObject = revitCurve };
      }
      catch (Exception)
      {
        throw (new Exception("Room boundary line creation failed\nView is not valid for room boundary line creation."));
      }


    }

    public SpaceSeparationLine SpaceSeparationLineToSpeckle(DB.ModelCurve revitCurve)
    {
      var speckleCurve = new SpaceSeparationLine(CurveToSpeckle(revitCurve.GeometryCurve));
      speckleCurve.elementId = revitCurve.Id.ToString();
      speckleCurve.applicationId = revitCurve.UniqueId;
      speckleCurve.units = ModelUnits;
      Report.Log($"Converted SpaceSeparationLine {revitCurve.Id}");
      return speckleCurve;
    }

    public ApplicationPlaceholderObject SpaceSeparationLineToNative(SpaceSeparationLine speckleCurve)
    {
      var docObj = GetExistingElementByApplicationId(speckleCurve.applicationId);
      if (docObj != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore)
        return new ApplicationPlaceholderObject
        { applicationId = speckleCurve.applicationId, ApplicationGeneratedId = docObj.UniqueId, NativeObject = docObj };
      var baseCurve = CurveToNative(speckleCurve.baseCurve);

      // try update existing (update model curve geometry curve based on speckle curve)
      if (docObj != null)
      {
        try
        {
          var docCurve = docObj as DB.ModelCurve;
          var revitGeom = docCurve.GeometryCurve;
          var speckleGeom = baseCurve.get_Item(0);
          bool fullOverlap = speckleGeom.Intersect(revitGeom) == SetComparisonResult.Equal;
          if (!fullOverlap)
          {
            docCurve.SetGeometryCurve(speckleGeom, false);
          }
          Report.Log($"Updated SpaceSeparationLine {docCurve.Id}");
          return new ApplicationPlaceholderObject()
          { applicationId = speckleCurve.applicationId, ApplicationGeneratedId = docCurve.UniqueId, NativeObject = docCurve };
        }
        catch
        {
          //delete and try to create new line as fallback
          Doc.Delete(docObj.Id);
        }
      }

      try
      {
        var res = Doc.Create.NewSpaceBoundaryLines(NewSketchPlaneFromCurve(baseCurve.get_Item(0), Doc), baseCurve, Doc.ActiveView).get_Item(0);
        Report.Log($"Created SpaceSeparationLine {res.Id}");
        return new ApplicationPlaceholderObject()
        { applicationId = speckleCurve.applicationId, ApplicationGeneratedId = res.UniqueId, NativeObject = res };
      }
      catch (Exception)
      {
        throw (new Exception("Space separation line creation failed\nView is not valid for space separation line creation."));
      }
    }

    /// <summary>
    /// Credits: Grevit
    /// Creates a new Sketch Plane from a Curve
    /// https://github.com/grevit-dev/Grevit/blob/3c7a5cc198e00dfa4cc1e892edba7c7afd1a3f84/Grevit.Revit/Utilities.cs#L402
    /// </summary>
    /// <param name="curve">Curve to get plane from</param>
    /// <returns>Plane of the curve</returns>
    private SketchPlane NewSketchPlaneFromCurve(DB.Curve curve, Document doc)
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

      return SketchPlane.Create(doc, plane);
    }
    private DB.Plane CreatePlane(XYZ basis, XYZ startPoint)
    {
      return DB.Plane.CreateByNormalAndOrigin(basis, startPoint);
    }
  }
}