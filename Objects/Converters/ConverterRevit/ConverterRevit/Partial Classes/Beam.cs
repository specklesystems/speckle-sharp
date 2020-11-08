using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Objects.Revit;
using System;

using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public DB.Element BeamToNative(RevitBeam speckleBeam)
    {
      return FamilyInstanceToNative(speckleBeam, StructuralType.Beam);
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