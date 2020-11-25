using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Objects.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public List<ApplicationPlaceholderObject> BeamToNative(IBeam speckleBeam, StructuralType structuralType = StructuralType.Beam)
    {
      if (speckleBeam.baseLine == null)
      {
        throw new Exception("Only line based Beams are currently supported.");
      }

      DB.FamilySymbol familySymbol = GetElementType<FamilySymbol>(speckleBeam as Base);
      var baseLine = CurveToNative(speckleBeam.baseLine).get_Item(0);
      DB.Level level = null;
      DB.FamilyInstance revitBeam = null;

      //comes from revit or schema builder, has these props
      var speckleRevitBeam = speckleBeam as RevitBeam;
      
      if (speckleRevitBeam != null)
      {
        level = GetLevelByName(speckleRevitBeam.level.name);
      }

      if (level == null)
      {
        level = LevelToNative(LevelFromCurve(baseLine));
      }

      //try update existing 
      var docObj = GetExistingElementByApplicationId(((Base)speckleBeam).applicationId);

      if (docObj != null)
      {
        try
        {
          var revitType = Doc.GetElement(docObj.GetTypeId()) as ElementType;

          // if family changed, tough luck. delete and let us create a new one.
          if (familySymbol.FamilyName != revitType.FamilyName)
          {
            Doc.Delete(docObj.Id);
          }
          else
          {
            revitBeam = (DB.FamilyInstance)docObj;
            (revitBeam.Location as LocationCurve).Curve = baseLine;

            // check for a type change
            if (!string.IsNullOrEmpty(familySymbol.FamilyName) && familySymbol.FamilyName != revitType.Name)
            {
              revitBeam.ChangeTypeId(familySymbol.Id);
            }
          }
        }
        catch
        {
          //something went wrong, re-create it
        }
      }

      //create family instance
      if (revitBeam == null)
      {
        revitBeam = Doc.Create.NewFamilyInstance(baseLine, familySymbol, level, structuralType);
      }

      //reference level, only for beams
      TrySetParam(revitBeam, BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM, level);

      if (speckleRevitBeam != null)
      {
        SetElementParamsFromSpeckle(revitBeam, speckleRevitBeam);
      }

      // TODO: get sub families, it's a family! 
      var placeholders = new List<ApplicationPlaceholderObject>() { new ApplicationPlaceholderObject { applicationId = ((Base)speckleBeam).applicationId, ApplicationGeneratedId = revitBeam.UniqueId } };

      // TODO: nested elements.

      return placeholders;
    }

    private RevitBeam BeamToSpeckle(DB.FamilyInstance revitBeam)
    {
      var baseGeometry = LocationToSpeckle(revitBeam);
      var baseLine = baseGeometry as ICurve;
      if (baseLine == null)
      {
        throw new Exception("Only line based Beams are currently supported.");
      }

      var baseLevelParam = revitBeam.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);

      var speckleBeam = new RevitBeam();
      speckleBeam.type = Doc.GetElement(revitBeam.GetTypeId()).Name;
      speckleBeam.baseLine = baseLine;
      speckleBeam.level = ConvertAndCacheLevel(baseLevelParam);
      speckleBeam["@displayMesh"] = GetElementMesh(revitBeam);

      AddCommonRevitProps(speckleBeam, revitBeam);

      return speckleBeam;
    }
  }
}