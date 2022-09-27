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
    #region speckle to native
    public ApplicationObject CreateAppObject(string id, string applicationId, string speckle_type)
    {
      var docObjs = GetExistingElementsByApplicationId(applicationId);
      var appObj = new ApplicationObject(id, speckle_type) { applicationId = applicationId };

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(docObjs.FirstOrDefault(), appObj, out appObj))
        return appObj;

      foreach (var docObj in docObjs)
      {
        if (docObj != null)
        {
          // TODO: try updating lines
          Doc.Delete(docObj.Id);
        }
      }

      return appObj;
    }

    public ApplicationObject AlignmentToNative(Alignment alignment)
    {
      var appObj = CreateAppObject(alignment.id, alignment.applicationId, alignment.speckle_type);
      if (appObj.Status == ApplicationObject.State.Skipped)
        return appObj;

      var curves = CurveToNative(alignment.curves);
      var curveEnumerator = curves.GetEnumerator();
      while (curveEnumerator.MoveNext() && curveEnumerator.Current != null)
      {
        var baseCurve = curveEnumerator.Current as DB.Curve;
        DB.ModelCurve revitCurve = Doc.Create.NewModelCurve(baseCurve, NewSketchPlaneFromCurve(baseCurve, Doc));
        appObj.Update(createdId: revitCurve.UniqueId);
      }
      appObj.Update(status: ApplicationObject.State.Created);
      return appObj;
    }

    public ApplicationObject DetailCurveToNative(DetailCurve speckleCurve)
    {
      var appObj = CreateAppObject(speckleCurve.id, speckleCurve.applicationId, speckleCurve.speckle_type);
      if (appObj.Status == ApplicationObject.State.Skipped)
        return appObj;

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
          appObj.Update(logItem: $"Detail curve creation failed\nView is not valid for detail curve creation.");
          continue;
        }

        var lineStyles = revitCurve.GetLineStyleIds();
        var lineStyleId = lineStyles.FirstOrDefault(x => Doc.GetElement(x).Name == speckleCurve.lineStyle);
        if (lineStyleId != null)
          revitCurve.LineStyle = Doc.GetElement(lineStyleId);

        appObj.Update(createdId: revitCurve.UniqueId, convertedItem: revitCurve);
      }
      appObj.Update(status: ApplicationObject.State.Created, logItem: $"Created as {appObj.CreatedIds.Count} detail curves");
      return appObj;
    }

    public ApplicationObject ModelCurveToNative(ModelCurve speckleCurve)
    {
      var appObj = CreateAppObject(speckleCurve.id, speckleCurve.applicationId, speckleCurve.speckle_type);
      if (appObj.Status == ApplicationObject.State.Skipped)
        return appObj;

      var curves = CurveToNative(speckleCurve.baseCurve);
      var curveEnumerator = curves.GetEnumerator();
      while (curveEnumerator.MoveNext() && curveEnumerator.Current != null)
      {
        var baseCurve = curveEnumerator.Current as DB.Curve;
        DB.ModelCurve revitCurve = Doc.Create.NewModelCurve(baseCurve, NewSketchPlaneFromCurve(baseCurve, Doc));

        var lineStyles = revitCurve.GetLineStyleIds();
        var lineStyleId = lineStyles.FirstOrDefault(x => Doc.GetElement(x).Name == speckleCurve.lineStyle);
        if (lineStyleId != null)
          revitCurve.LineStyle = Doc.GetElement(lineStyleId);

        appObj.Update(createdId: revitCurve.UniqueId, convertedItem: revitCurve);
      }
      appObj.Update(status: ApplicationObject.State.Created);
      return appObj;
    }

    // This is to support raw geometry being sent to Revit (eg from rhino, gh, autocad...)
    public ApplicationObject ModelCurveToNative(ICurve speckleLine)
    {
      // if it comes from GH it doesn't have an applicationId, the use the hash id
      if ((speckleLine as Base).applicationId == null)
        (speckleLine as Base).applicationId = (speckleLine as Base).id;

      var speckleCurve = speckleLine as Base;
      var appObj = CreateAppObject(speckleCurve.id, speckleCurve.applicationId, speckleCurve.speckle_type);
      if (appObj.Status == ApplicationObject.State.Skipped)
        return appObj;

      try
      {
        return ModelCurvesFromEnumerator(CurveToNative(speckleLine).GetEnumerator(), speckleLine, appObj);
      }
      catch (Exception e)
      {
        // use display value if curve fails (prob a closed, periodic curve or a non-planar nurbs)
        if (speckleLine is IDisplayValue<Geometry.Polyline> d)
        {
          appObj.Update(logItem: $"Curve failed conversion (probably a closed period curve or non-planar nurbs): used polyline display value instead.");
          return ModelCurvesFromEnumerator(CurveToNative(d.displayValue).GetEnumerator(), speckleLine, appObj);
        }
        else
        {
          appObj.Update(status: ApplicationObject.State.Failed, logItem: e.Message);
          return appObj;
        }
      }
    }

    public ApplicationObject RoomBoundaryLineToNative(RoomBoundaryLine speckleCurve)
    {
      var appObj = CreateAppObject(speckleCurve.id, speckleCurve.applicationId, speckleCurve.speckle_type);
      if (appObj.Status == ApplicationObject.State.Skipped)
        return appObj;

      var baseCurve = CurveToNative(speckleCurve.baseCurve);

      try
      {
        var revitCurve = Doc.Create.NewRoomBoundaryLines(NewSketchPlaneFromCurve(baseCurve.get_Item(0), Doc), baseCurve, Doc.ActiveView).get_Item(0);
        appObj.Update(status: ApplicationObject.State.Created, createdId: revitCurve.UniqueId, convertedItem: revitCurve);
      }
      catch (Exception)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "View is not valid for room boundary line creation.");
      }
      return appObj;
    }

    public ApplicationObject SpaceSeparationLineToNative(SpaceSeparationLine speckleCurve)
    {
      var appObj = CreateAppObject(speckleCurve.id, speckleCurve.applicationId, speckleCurve.speckle_type);
      if (appObj.Status == ApplicationObject.State.Skipped)
        return appObj;

      var baseCurve = CurveToNative(speckleCurve.baseCurve);

      // try update existing (update model curve geometry curve based on speckle curve)
      //if (docObj != null)
      //{
      //  try
      //  {
      //    var docCurve = docObj as DB.ModelCurve;
      //    var revitGeom = docCurve.GeometryCurve;
      //    var speckleGeom = baseCurve.get_Item(0);
      //    bool fullOverlap = speckleGeom.Intersect(revitGeom) == SetComparisonResult.Equal;
      //    if (!fullOverlap)
      //      docCurve.SetGeometryCurve(speckleGeom, false);

      //    appObj.Update(status: ApplicationObject.State.Updated, createdId: docCurve.UniqueId, convertedItem: docCurve);
      //    return appObj;
      //  }
      //  catch
      //  {
      //    //delete and try to create new line as fallback
      //    Doc.Delete(docObj.Id);
      //  }
      //}

      try
      {
        var res = Doc.Create.NewSpaceBoundaryLines(NewSketchPlaneFromCurve(baseCurve.get_Item(0), Doc), baseCurve, Doc.ActiveView).get_Item(0);
        appObj.Update(status: ApplicationObject.State.Created, createdId: res.UniqueId, convertedItem: res);
      }
      catch (Exception)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "View is not valid for space separation line creation.");
      }
      return appObj;
    }

    public ApplicationObject ModelCurvesFromEnumerator(IEnumerator curveEnum, ICurve speckleLine, ApplicationObject appObj)
    {
      while (curveEnum.MoveNext() && curveEnum.Current != null)
      {
        var curve = curveEnum.Current as DB.Curve;
        // Curves must be bound in order to be valid model curves
        if (!curve.IsBound) curve.MakeBound(speckleLine.domain.start ?? 0, speckleLine.domain.end ?? Math.PI * 2);
        DB.ModelCurve revitCurve = null;

        if (Doc.IsFamilyDocument)
          revitCurve = Doc.FamilyCreate.NewModelCurve(curve, NewSketchPlaneFromCurve(curve, Doc));
        else
          revitCurve = Doc.Create.NewModelCurve(curve, NewSketchPlaneFromCurve(curve, Doc));

        if (revitCurve != null)
          appObj.Update(createdId: revitCurve.UniqueId, convertedItem: revitCurve);
      }
      if (appObj.CreatedIds.Count > 1) appObj.Update(logItem: $"Created as {appObj.CreatedIds.Count} model curves");
      appObj.Update(status: ApplicationObject.State.Created);
      return appObj;
    }

    #endregion

    #region native to speckle

    public ModelCurve ModelCurveToSpeckle(DB.ModelCurve revitCurve)
    {
      var speckleCurve = new ModelCurve(CurveToSpeckle(revitCurve.GeometryCurve), revitCurve.LineStyle.Name);
      speckleCurve.elementId = revitCurve.Id.ToString();
      speckleCurve.applicationId = revitCurve.UniqueId;
      speckleCurve.units = ModelUnits;
      return speckleCurve;
    }

    public DetailCurve DetailCurveToSpeckle(DB.DetailCurve revitCurve)
    {
      var speckleCurve = new DetailCurve(CurveToSpeckle(revitCurve.GeometryCurve), revitCurve.LineStyle.Name);
      speckleCurve.elementId = revitCurve.Id.ToString();
      speckleCurve.applicationId = revitCurve.UniqueId;
      speckleCurve.units = ModelUnits;
      return speckleCurve;
    }

    public RoomBoundaryLine RoomBoundaryLineToSpeckle(DB.ModelCurve revitCurve)
    {
      var speckleCurve = new RoomBoundaryLine(CurveToSpeckle(revitCurve.GeometryCurve));
      speckleCurve.elementId = revitCurve.Id.ToString();
      speckleCurve.applicationId = revitCurve.UniqueId;
      speckleCurve.units = ModelUnits;
      return speckleCurve;
    }

    public SpaceSeparationLine SpaceSeparationLineToSpeckle(DB.ModelCurve revitCurve)
    {
      var speckleCurve = new SpaceSeparationLine(CurveToSpeckle(revitCurve.GeometryCurve));
      speckleCurve.elementId = revitCurve.Id.ToString();
      speckleCurve.applicationId = revitCurve.UniqueId;
      speckleCurve.units = ModelUnits;
      return speckleCurve;
    }

    #endregion

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
        plane = DB.Plane.CreateByNormalAndOrigin(XYZ.BasisZ, startPoint);

      // If X Values are equal the Plane is YZ
      else if (startPoint.X == endPoint.X)
        plane = DB.Plane.CreateByNormalAndOrigin(XYZ.BasisX, startPoint);

      // If Y Values are equal the Plane is XZ
      else if (startPoint.Y == endPoint.Y)
        plane = DB.Plane.CreateByNormalAndOrigin(XYZ.BasisY, startPoint);

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
  }
}