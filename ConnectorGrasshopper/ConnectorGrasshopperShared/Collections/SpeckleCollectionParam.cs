using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace ConnectorGrasshopper.Collections;

public class SpeckleCollectionParam : GH_Param<GH_SpeckleCollection>
{
  public SpeckleCollectionParam()
    : base("Speckle Collection", "SC", "A Speckle collection object", "Params", "Primitive", GH_ParamAccess.item) { }

  public override Guid ComponentGuid => new("96F52497-6E5B-4941-9350-D6C87F0EA1E3");

  protected override Bitmap Icon => Properties.Resources.SpeckleCollection;

  public override GH_Exposure Exposure => GH_Exposure.septenary;
}
