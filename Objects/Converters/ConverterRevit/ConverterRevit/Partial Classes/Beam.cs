using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Objects.BuiltElements;
using Objects.Revit;
using System;

using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public DB.Element BeamToNative(Beam speckleBeam)
    {
      if (speckleBeam.baseLine == null)
      {
        throw new Exception("Only line based Beams are currently supported.");
      }

      string familyName = "";
      DB.FamilySymbol familySymbol = null;
      var baseLine = CurveToNative(speckleBeam.baseLine).get_Item(0);
      DB.Level level = null;
      DB.FamilyInstance revitBeam = null;

      //comes from revit or schema builder, has these props
      if (speckleBeam is RevitBeam rb)
      {
        familyName = rb.family;
        familySymbol = GetFamilySymbol(rb);
        level = LevelToNative(rb.level);
      }
      else
      {
        level = LevelToNative(LevelFromCurve(baseLine));
      }

      //try update existing 
      var (docObj, stateObj) = GetExistingElementByApplicationId(speckleBeam.applicationId, speckleBeam.speckle_type);
      if (docObj != null)
      {
        try
        {
          var revitType = Doc.GetElement(docObj.GetTypeId()) as ElementType;

          // if family changed, tough luck. delete and let us create a new one.
          if (familyName != revitType.FamilyName)
          {
            Doc.Delete(docObj.Id);
          }
          else
          {
            revitBeam = (DB.FamilyInstance)docObj;


            (revitBeam.Location as LocationCurve).Curve = baseLine;
            //else
            //(revitBeam.Location as LocationPoint).Point = location as XYZ;

            // check for a type change
            if (!string.IsNullOrEmpty(familyName) && familyName != revitType.Name)
              revitBeam.ChangeTypeId(familySymbol.Id);
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
        revitBeam = Doc.Create.NewFamilyInstance(baseLine, familySymbol, level, StructuralType.Beam);
        //else
        //  revitBeam = Doc.Create.NewFamilyInstance(location as XYZ, familySymbol, level, structuralType);
      }

      //reference level, only for beams
      TrySetParam(revitBeam, BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM, level);


      if (speckleBeam is IRevit item)
        SetElementParams(revitBeam, item);

      return revitBeam;
    }

    private IRevitElement BeamToSpeckle(DB.FamilyInstance revitBeam)
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
      speckleBeam.level = (RevitLevel)ParameterToSpeckle(baseLevelParam);
      speckleBeam.displayMesh = MeshUtils.GetElementMesh(revitBeam, Scale);

      AddCommonRevitProps(speckleBeam, revitBeam);

      return speckleBeam;
    }
  }
}