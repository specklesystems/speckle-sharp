using Autodesk.Revit.DB;
using ConverterRevitShared.Revit;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    //TODO: delete temp family after creation
    //TODO: allow updates to family..?

    //NOTE: FaceWalls cannot be updated, as well we can't seem to get their base face easily so they are ToNatvie only
    public List<ApplicationPlaceholderObject> FaceWallToNative(RevitFaceWall speckleWall)
    {
      if (speckleWall.surface == null)
      {
        throw new Speckle.Core.Logging.SpeckleException("Only surface based FaceWalls are currently supported.");
      }

      // Cannot update revit wall to new mass face
      FaceWall revitWall = GetExistingElementByApplicationId(speckleWall.applicationId) as FaceWall;
      if (revitWall != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore)
        return new List<ApplicationPlaceholderObject> { new ApplicationPlaceholderObject { applicationId = speckleWall.applicationId, ApplicationGeneratedId = revitWall.UniqueId, NativeObject = revitWall } };

      if (revitWall != null)
      {
        Doc.Delete(revitWall.Id);
      }

      var templatePath = GetTemplatePath("Mass");
      if (!File.Exists(templatePath))
      {
        Report.LogConversionError(new Exception($"Could not find file {Path.GetFileName(templatePath)}"));
        return null;
      }

      var tempMassFamilyPath = CreateMassFamily(templatePath, speckleWall.surface, speckleWall.applicationId);
      Family fam;
      Doc.LoadFamily(tempMassFamilyPath, new FamilyLoadOption(), out fam);
      var symbol = Doc.GetElement(fam.GetFamilySymbolIds().First()) as FamilySymbol;
      symbol.Activate();

      try
      {
        File.Delete(tempMassFamilyPath);
      }
      catch
      {

      }

      var mass = Doc.Create.NewFamilyInstance(XYZ.Zero, symbol, DB.Structure.StructuralType.NonStructural);
      // NOTE: must set a schedule level!
      // otherwise the wall creation will fail with "Could not create a face wall."
      var level = new FilteredElementCollector(Doc)
        .WhereElementIsNotElementType()
        .OfCategory(BuiltInCategory.OST_Levels) // this throws a null error if user tries to recieve stream in a file with no levels
        .ToElements().FirstOrDefault();

      if (level == null) // create a new level at 0 if no levels could be retrieved from doc
        level = Level.Create(Doc, 0);

      TrySetParam(mass, BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM, level);

      //must regenerate before getting the elem geometry
      Doc.Regenerate();
      Reference faceRef = GetFaceRef(mass);

      var wallType = GetElementType<WallType>(speckleWall);
      if (!FaceWall.IsWallTypeValidForFaceWall(Doc, wallType.Id))
      {
        Report.LogConversionError(new Exception($"Wall type not valid for face wall ${speckleWall.applicationId}."));
        return null;
      }

      revitWall = null;
      try
      {
        revitWall = DB.FaceWall.Create(Doc, wallType.Id, GetWallLocationLine(speckleWall.locationLine), faceRef);
      }
      catch (Exception e)
      { }

      if (revitWall == null)
      {
        throw (new Exception($"Failed to create face wall ${speckleWall.applicationId}."));
      }

      Doc.Delete(mass.Id);

      SetInstanceParameters(revitWall, speckleWall);

      var placeholders = new List<ApplicationPlaceholderObject>()
      {
        new ApplicationPlaceholderObject
        {
        applicationId = speckleWall.applicationId,
        ApplicationGeneratedId = revitWall.UniqueId,
        NativeObject = revitWall
        }
      };

      var hostedElements = SetHostedElements(speckleWall, revitWall);
      placeholders.AddRange(hostedElements);
      Report.Log($"Created FaceWall {revitWall.Id}");
      return placeholders;
    }

    private Reference GetFaceRef(Element e)
    {
      Options geomOption = e.Document.Application.Create.NewGeometryOptions();
      geomOption.ComputeReferences = true;
      geomOption.IncludeNonVisibleObjects = true;
      geomOption.DetailLevel = ViewDetailLevel.Fine;

      GeometryElement ge = e.get_Geometry(geomOption);

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

      using (Transaction t = new Transaction(famDoc, "Create Mass"))
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
        catch (Exception e)
        {

        }

        t.Commit();

      }
      var famName = "SpeckleMass_" + name;
      string tempFamilyPath = Path.Combine(Path.GetTempPath(), famName + ".rfa");
      SaveAsOptions so = new SaveAsOptions();
      so.OverwriteExistingFile = true;
      famDoc.SaveAs(tempFamilyPath, so);
      famDoc.Close();
      Report.Log($"Created temp family {tempFamilyPath}");
      return tempFamilyPath;
    }

  }

}
