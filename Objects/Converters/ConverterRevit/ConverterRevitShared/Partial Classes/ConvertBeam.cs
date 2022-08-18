using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    // CAUTION: this string needs to have the same values as in the connector
    const string StructuralFraming = "Structural Framing";

    public List<ApplicationPlaceholderObject> BeamToNative(Beam speckleBeam, StructuralType structuralType = StructuralType.Beam)
    {
      
      if (speckleBeam.baseLine == null)
      {
        throw new Speckle.Core.Logging.SpeckleException("Only line based Beams are currently supported.");
      }

      DB.FamilySymbol familySymbol = GetElementType<FamilySymbol>(speckleBeam);
      var baseLine = CurveToNative(speckleBeam.baseLine).get_Item(0);
      DB.Level level = null;
      DB.FamilyInstance revitBeam = null;

      //comes from revit or schema builder, has these props
      var speckleRevitBeam = speckleBeam as RevitBeam;

      if (speckleRevitBeam != null)
      {
        if (level != null)
        {
          level = GetLevelByName(speckleRevitBeam.level.name);
        }
      }

      level ??= ConvertLevelToRevit(speckleRevitBeam?.level ?? LevelFromCurve(baseLine));
      var isUpdate = false;
      //try update existing 
      var docObj = GetExistingElementByApplicationId(speckleBeam.applicationId);

      if (docObj != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore)
        return new List<ApplicationPlaceholderObject>() { new ApplicationPlaceholderObject { applicationId = speckleBeam.applicationId, ApplicationGeneratedId = docObj.UniqueId, NativeObject = docObj } }; ;

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
          isUpdate = true;
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
        // check for disallow join for beams in user settings
        // currently, this setting only applies to beams being created
        if (Settings.ContainsKey("disallow-join"))
        {
          List<string> joinSettings = new List<string>(Regex.Split(Settings["disallow-join"], @"\,\ "));
          if (joinSettings.Contains(StructuralFraming))
          {
            StructuralFramingUtils.DisallowJoinAtEnd(revitBeam, 0);
            StructuralFramingUtils.DisallowJoinAtEnd(revitBeam, 1);
          }
        }
      }

      //reference level, only for beams
      TrySetParam(revitBeam, BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM, level);

      if (speckleRevitBeam != null)
      {
        SetInstanceParameters(revitBeam, speckleRevitBeam);
      }

      // TODO: get sub families, it's a family! 
      var placeholders = new List<ApplicationPlaceholderObject>() { new ApplicationPlaceholderObject { applicationId = speckleBeam.applicationId, ApplicationGeneratedId = revitBeam.UniqueId, NativeObject = revitBeam } };

      // TODO: nested elements.

      Report.Log($"{(isUpdate ? "Updated" : "Created")} AdaptiveComponent {revitBeam.Id}");

      return placeholders;
    }

    private Base BeamToSpeckle(DB.FamilyInstance revitBeam)
    {
      var baseGeometry = LocationToSpeckle(revitBeam);
      var baseLine = baseGeometry as ICurve;
      if (baseLine == null)
      {
        Report.Log($"Beam has no valid baseline, converting as generic element {revitBeam.Id}");
        return RevitElementToSpeckle(revitBeam);
      }
      var symbol = revitBeam.Document.GetElement(revitBeam.GetTypeId()) as FamilySymbol;

      var speckleBeam = new RevitBeam();
      speckleBeam.family = symbol.FamilyName;
      speckleBeam.type = revitBeam.Document.GetElement(revitBeam.GetTypeId()).Name;
      speckleBeam.baseLine = baseLine;
      speckleBeam.level = ConvertAndCacheLevel(revitBeam, BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);
      speckleBeam.displayValue = GetElementMesh(revitBeam);

      GetAllRevitParamsAndIds(speckleBeam, revitBeam);

      Report.Log($"Converted Beam {revitBeam.Id}");
      return speckleBeam;
    }
  }
}
