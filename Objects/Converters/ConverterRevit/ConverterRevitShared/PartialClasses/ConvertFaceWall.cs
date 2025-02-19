using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using ConverterRevitShared.Revit;
using Objects.BuiltElements.Revit;
using RevitSharedResources.Extensions.SpeckleExtensions;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  //TODO: delete temp family after creation
  //TODO: allow updates to family..?

  //NOTE: FaceWalls cannot be updated, as well we can't seem to get their base face easily so they are ToNatvie only
  public ApplicationObject FaceWallToNative(RevitFaceWall speckleWall)
  {
    FaceWall revitWall = GetExistingElementByApplicationId(speckleWall.applicationId) as FaceWall;
    var appObj = new ApplicationObject(speckleWall.id, speckleWall.speckle_type)
    {
      applicationId = speckleWall.applicationId
    };

    // skip if element already exists in doc & receive mode is set to ignore
    if (IsIgnore(revitWall, appObj))
    {
      return appObj;
    }

    if (speckleWall?.brep?.Surfaces?.FirstOrDefault() == null)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "FaceWall surface was null");
      return appObj;
    }

    if (revitWall != null)
    {
      Doc.Delete(revitWall.Id);
    }

    var templatePath = GetTemplatePath("Mass");
    if (!File.Exists(templatePath))
    {
      appObj.Update(
        status: ApplicationObject.State.Failed,
        logItem: $"Could not find file {Path.GetFileName(templatePath)}"
      );
      return appObj;
    }

    var tempMassFamilyPath = CreateMassFamily(
      templatePath,
      speckleWall.brep.Surfaces.First(),
      speckleWall.applicationId
    );

    Doc.LoadFamily(tempMassFamilyPath, new FamilyLoadOption(), out Family fam);
    var symbol = Doc.GetElement(fam.GetFamilySymbolIds().First()) as FamilySymbol;
    symbol.Activate();

    if (File.Exists(tempMassFamilyPath))
    {
      File.Delete(tempMassFamilyPath);
    }

    var mass = Doc.Create.NewFamilyInstance(XYZ.Zero, symbol, DB.Structure.StructuralType.NonStructural);
    // NOTE: must set a schedule level!
    // otherwise the wall creation will fail with "Could not create a face wall."
    var level = new FilteredElementCollector(Doc)
      .WhereElementIsNotElementType()
      .OfCategory(BuiltInCategory.OST_Levels) // this throws a null error if user tries to recieve stream in a file with no levels
      .ToElements()
      .FirstOrDefault();

    if (level == null) // create a new level at 0 if no levels could be retrieved from doc
    {
      level = Level.Create(Doc, 0);
    }

    TrySetParam(mass, BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM, level);

    //must regenerate before getting the elem geometry
    Doc.Regenerate();
    Reference faceRef = GetFaceRef(mass);

    var wallType = GetElementType<WallType>(speckleWall, appObj, out bool _);
    if (wallType == null)
    {
      appObj.Update(status: ApplicationObject.State.Failed);
      return appObj;
    }
    if (!FaceWall.IsWallTypeValidForFaceWall(Doc, wallType.Id))
    {
      appObj.Update(
        status: ApplicationObject.State.Failed,
        logItem: $"Wall type {wallType.Name} not valid for facewall"
      );
      return appObj;
    }

    revitWall = null;
    try
    {
      revitWall = DB.FaceWall.Create(Doc, wallType.Id, GetWallLocationLine(speckleWall.locationLine), faceRef);
    }
    catch (Autodesk.Revit.Exceptions.ApplicationException) { }

    if (revitWall == null)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "Revit wall creation returned null");
      return appObj;
    }

    Doc.Delete(mass.Id);

    SetInstanceParameters(revitWall, speckleWall);
    appObj.Update(status: ApplicationObject.State.Created, createdId: revitWall.UniqueId, convertedItem: revitWall);
    //appObj = SetHostedElements(speckleWall, revitWall, appObj);
    return appObj;
  }

  public ApplicationObject FaceWallToNativeV2(RevitFaceWall speckleWall)
  {
    var appObj = new ApplicationObject(speckleWall.id, speckleWall.speckle_type)
    {
      applicationId = speckleWall.applicationId
    };

    try
    {
      var existing = GetExistingElementByApplicationId(speckleWall.applicationId) as FaceWall;

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(existing, appObj))
      {
        return appObj;
      }

      if (speckleWall.brep == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "FaceWall geometry was null");
        return appObj;
      }

      if (existing != null)
      {
        Doc.Delete(existing.Id);
      }

      var wallType = GetElementType<WallType>(speckleWall, appObj, out bool _);
      if (wallType == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed);
        return appObj;
      }

      if (!FaceWall.IsWallTypeValidForFaceWall(Doc, wallType.Id))
      {
        appObj.Update(
          status: ApplicationObject.State.Failed,
          logItem: $"Wall type {wallType.Name} not valid for FaceWall"
        );
        return appObj;
      }

      List<string> notes = null;
      Solid solid = BrepToNative(speckleWall.brep, out notes);
      DB.FamilyInstance freeform = CreateFreeformElementFamily(new List<Solid> { solid }, speckleWall.id, "Mass");
      Doc.Regenerate();
      Reference faceref = GetFaceRef(freeform);

      var revitWall = FaceWall.Create(Doc, wallType.Id, GetWallLocationLine(speckleWall.locationLine), faceref);

      SetInstanceParameters(revitWall, speckleWall);
      appObj.Update(status: ApplicationObject.State.Created, createdId: revitWall.UniqueId, convertedItem: revitWall);
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.LogDefaultError(ex);
      appObj.Update(
        status: ApplicationObject.State.Failed,
        logItem: $"Revit wall creation failed: {ex.Message}",
        log: new List<string> { ex.ToFormattedString() }
      );
    }

    return appObj;
  }

  private Reference GetFaceRef(Element e)
  {
    var geomOption = e.Document.Application.Create.NewGeometryOptions();
    geomOption.ComputeReferences = true;
    geomOption.IncludeNonVisibleObjects = true;
    geomOption.DetailLevel = ViewDetailLevel.Fine;

    var ge = e.get_Geometry(geomOption);

    foreach (GeometryObject geomObj in ge)
    {
      Solid geomSolid = geomObj as Solid;
      if (null != geomSolid)
      {
        foreach (Face geomFace in geomSolid.Faces)
        {
          if (FaceWall.IsValidFaceReferenceForFaceWall(e.Document, geomFace.Reference))
          {
            return geomFace.Reference;
          }
        }
      }
    }
    return null;
  }

  private string CreateMassFamily(string famPath, Geometry.Surface surface, string name)
  {
    var famDoc = Doc.Application.NewFamilyDocument(famPath);

    using (Transaction t = new(famDoc, "Create Mass"))
    {
      t.Start();

      try
      {
        var pointLists = surface.GetControlPoints();
        var curveArray = new ReferenceArrayArray();

        foreach (var list in pointLists)
        {
          var arr = new ReferencePointArray();
          foreach (var point in list)
          {
            var refPt = famDoc.FamilyCreate.NewReferencePoint(PointToNative(point));
            arr.Append(refPt);
          }

          var curve = famDoc.FamilyCreate.NewCurveByPoints(arr);
          var referenceArray = new ReferenceArray();
          referenceArray.Append(curve.GeometryCurve.Reference);
          curveArray.Append(referenceArray);
        }

        var loft = famDoc.FamilyCreate.NewLoftForm(true, curveArray);
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        SpeckleLog.Logger.LogDefaultError(ex);
      }

      t.Commit();
    }
    var famName = "SpeckleMass_" + name;
    string tempFamilyPath = Path.Combine(Path.GetTempPath(), famName + ".rfa");
    SaveAsOptions so = new();
    so.OverwriteExistingFile = true;
    famDoc.SaveAs(tempFamilyPath, so);
    famDoc.Close();
    Report.Log($"Created temp family {tempFamilyPath}");
    return tempFamilyPath;
  }
}
