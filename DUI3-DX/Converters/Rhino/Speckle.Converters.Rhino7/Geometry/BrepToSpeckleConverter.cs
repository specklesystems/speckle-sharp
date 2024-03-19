namespace Speckle.Converters.Rhino7.Geometry;

public class BrepToSpeckleConverter
{
  public BrepToSpeckleConverter() { }

  private RG.Mesh GetBrepDisplayMesh(RG.Brep brep)
  {
    var joinedMesh = new RG.Mesh();

    RG.MeshingParameters mySettings = new(0.05, 0.05);

    // TODO: Mesh settings should be passed by context
    // switch (SelectedMeshSettings)
    // {
    //   case MeshSettings.CurrentDoc:
    //     mySettings = RH.MeshingParameters.DocumentCurrentSetting(Doc);
    //     break;
    //   case MeshSettings.Default:
    //   default:
    //     mySettings = ;
    //     break;
    // }

    // TODO: This was wrapped in an ugly try/catch that swallowed exceptions. I'd rather we fail fast.
    joinedMesh.Append(RG.Mesh.CreateFromBrep(brep, mySettings));
    return joinedMesh;
  }
}
