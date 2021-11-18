using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace ConnectorGrasshopper
{
  public class ConnectorGrasshopperInfo : GH_AssemblyInfo
  {
    public override string Name => ComponentCategories.PRIMARY_RIBBON;

    public override Bitmap Icon => Properties.Resources.speckle_logo;

    public override string Description => "Grasshopper connectors for Speckle 2.0";

    public override Guid Id => new Guid("074ee50d-763d-495a-8be2-6934897dd6b1");

    public override string AuthorName => "AEC Systems";
    
    
    public override string AuthorContact => "hello@speckle.systems";
  }
}
