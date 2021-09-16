using Autodesk.Revit.DB.Structure;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Rebar = Objects.BuiltElements.Rebar;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    /*
    public List<ApplicationPlaceholderObject> RebarToNative(Rebar speckleRebar)
    {
      if (speckleRebar.curves.Count == 0)
      {
        throw new Speckle.Core.Logging.SpeckleException("Rebar has no base curves");
      }

      DB.FamilySymbol familySymbol = GetElementType<DB.FamilySymbol>(speckleRebar);
      var curves = speckleRebar.curves.Select(o => CurveToNative(o).get_Item(0));
      DB.Level level = null;
      DB.FamilyInstance revitRebar = null;

      //comes from revit or schema builder, has these props
      var speckleRevitRebar = speckleRebar as RevitRebar;

      if (speckleRevitRebar != null)
      {
        level = GetLevelByName(speckleRevitRebar.level.name);
      }

      if (level == null)
      {
        level = LevelToNative(LevelFromCurve(baseLine));
      }

      //try update existing 
      var docObj = GetExistingElementByApplicationId(speckleRebar.applicationId);

      if (docObj != null)
      {
        try
        {
          var revitType = Doc.GetElement(docObj.GetTypeId()) as DB.ElementType;

          // if family changed, tough luck. delete and let us create a new one.
          if (familySymbol.FamilyName != revitType.FamilyName)
          {
            Doc.Delete(docObj.Id);
          }
          else
          {
            revitRebar = (DB.FamilyInstance)docObj;
            (revitRebar.Location as LocationCurve).Curve = baseLine;

            // check for a type change
            if (!string.IsNullOrEmpty(familySymbol.FamilyName) && familySymbol.FamilyName != revitType.Name)
            {
              revitRebar.ChangeTypeId(familySymbol.Id);
            }
          }
        }
        catch
        {
          //something went wrong, re-create it
        }
      }

      //create family instance
      if (revitRebar == null)
      {
        revitRebar = Doc.Create.NewFamilyInstance(baseLine, familySymbol, level, structuralType);
        StructuralFramingUtils.DisallowJoinAtEnd(revitRebar, 0);
        StructuralFramingUtils.DisallowJoinAtEnd(revitRebar, 1);
      }

      //reference level, only for beams
      TrySetParam(revitRebar, DB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM, level);

      if (speckleRevitRebar != null)
      {
        SetInstanceParameters(revitRebar, speckleRevitRebar);
      }

      // TODO: get sub families, it's a family! 
      var placeholders = new List<ApplicationPlaceholderObject>() { new ApplicationPlaceholderObject { applicationId = speckleRebar.applicationId, ApplicationGeneratedId = revitBeam.UniqueId, NativeObject = revitBeam } };

      // TODO: nested elements.

      return placeholders;
    }
    */

    private Base RebarToSpeckle(DB.Structure.Rebar revitRebar)
    {
      // get rebar centerline curves using transform
      RebarShapeDrivenAccessor accessor = revitRebar.GetShapeDrivenAccessor();
      var bars = revitRebar.GetCenterlineCurves(true, true, true, MultiplanarOption.IncludeOnlyPlanarCurves, revitRebar.NumberOfBarPositions - 1);
      var curves = new List<ICurve>();
      for (int i = 0; i < bars.Count; i++)
      {
        DB.Transform t = accessor.GetBarPositionTransform(i);
        var bar = bars[i].CreateTransformed(t);
        curves.Add(CurveToSpeckle(bar));
      }

      var speckleRebar = new RevitRebar();
      speckleRebar.type = Doc.GetElement(revitRebar.GetTypeId()).Name;
      speckleRebar.curves = curves;
      speckleRebar.level = ConvertAndCacheLevel(revitRebar, DB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);
      speckleRebar.displayMesh = GetElementMesh(revitRebar);
      speckleRebar.volume = revitRebar.Volume;

      GetAllRevitParamsAndIds(speckleRebar, revitRebar);

      return speckleRebar;
    }

  }
}