
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using Beam = Objects.Beam;
using Element = Objects.Element;
using Level = Objects.Level;
using Autodesk.Revit.DB.Structure;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public DB.Element BeamToNative(Beam speckleBeam)
    {
      return FamilyInstanceToNative(speckleBeam, StructuralType.Beam);
    }

    private Element BeamToSpeckle(FamilyInstance revitBeam)
    {
      var baseLevelParam = revitBeam.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);

      var speckleBeam = new Beam();
      speckleBeam.type = Doc.GetElement(revitBeam.GetTypeId()).Name;
      speckleBeam.baseGeometry = LocationToSpeckle(revitBeam);
      speckleBeam.level = (Level)ParameterToSpeckle(baseLevelParam);
      speckleBeam.displayMesh  = MeshUtils.GetElementMesh(revitBeam, Scale);

      AddCommonRevitProps(speckleBeam, revitBeam);

      return speckleBeam;
    }


  
  }
}
