using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    // CAUTION: this string needs to have the same values as in the connector
    const string StructuralFraming = "Structural Framing";

    public ApplicationObject BeamToNative(Beam speckleBeam, StructuralType structuralType = StructuralType.Beam)
    {
      var docObj = GetExistingElementByApplicationId(speckleBeam.applicationId);
      var appObj = new ApplicationObject(speckleBeam.id, speckleBeam.speckle_type) { applicationId = speckleBeam.applicationId };

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(docObj, appObj))
        return appObj;

      if (speckleBeam.baseLine == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Only line based Beams are currently supported.");
        return appObj;
      }

      var familySymbol = GetElementType<FamilySymbol>(speckleBeam, appObj, out bool isExactMatch);
      if (familySymbol == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed);
        return appObj;
      }

      var baseLine = CurveToNative(speckleBeam.baseLine).get_Item(0);
      DB.Level level = null;
      DB.FamilyInstance revitBeam = null;

      //comes from revit or schema builder, has these props
      var speckleRevitBeam = speckleBeam as RevitBeam;
      if (speckleRevitBeam != null)
        if (level != null)
          level = GetLevelByName(speckleRevitBeam.level.name);

      level ??= ConvertLevelToRevit(speckleRevitBeam?.level ?? LevelFromCurve(baseLine), out ApplicationObject.State levelState);
      var isUpdate = false;

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
            if (isExactMatch && revitType.Id.IntegerValue != familySymbol.Id.IntegerValue)
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

        if (Settings.ContainsKey("disallow-join") && !string.IsNullOrEmpty(Settings["disallow-join"]))
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
        SetInstanceParameters(revitBeam, speckleRevitBeam);

      // TODO: get sub families, it's a family! 
      var state = isUpdate ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
      appObj.Update(status: state, createdId: revitBeam.UniqueId, convertedItem: revitBeam);
      appObj = SetHostedElements(speckleBeam, revitBeam, appObj);
      return appObj;
    }

    private Base BeamToSpeckle(DB.FamilyInstance revitBeam, out List<string> notes)
    {
      notes = new List<string>();
      var baseGeometry = LocationToSpeckle(revitBeam);
      var baseLine = baseGeometry as ICurve;
      if (baseLine == null)
      {
        notes.Add($"Beam has no valid baseline, converting as generic element");
        return RevitElementToSpeckle(revitBeam, out notes);
      }
      var symbol = revitBeam.Document.GetElement(revitBeam.GetTypeId()) as FamilySymbol;

      var speckleBeam = new RevitBeam();
      speckleBeam.family = symbol.FamilyName;
      speckleBeam.type = revitBeam.Document.GetElement(revitBeam.GetTypeId()).Name;
      speckleBeam.baseLine = baseLine;
      speckleBeam.level = ConvertAndCacheLevel(revitBeam, BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);

      // structural connection modifiers alter family instance geometry, but the modifiers are view specific
      // so we need to pass in the view we want in order to get the correct geometry
      // TODO: we need to make sure we are passing in the correct view
      var connectionHandlerFilter = new ElementClassFilter(typeof(DB.Structure.StructuralConnectionHandler));
      var options = revitBeam.GetSubelements().Where(o => (BuiltInCategory)o.Category.Id.IntegerValue == DB.BuiltInCategory.OST_StructConnectionModifiers).Any() || revitBeam.GetDependentElements(connectionHandlerFilter).Any() ?
        new Options() { View = Doc.ActiveView, ComputeReferences = true } : SolidDisplayValueOptions;
      speckleBeam.displayValue = GetElementDisplayValue(revitBeam, options);

      GetAllRevitParamsAndIds(speckleBeam, revitBeam);

      return speckleBeam;
    }
}
  }
