using System;
using System.Drawing;
using Grasshopper.Kernel;
using Speckle.Core.Kits;
using Speckle.Core.Logging;

namespace ConnectorGrasshopper
{
  public class ConnectorGrasshopperInfo : GH_AssemblyInfo
  {
    public override string Name => "Speckle 2";

    public override Bitmap Icon => Properties.Resources.speckle_logo;

    public override string Description => "Grasshopper connectors for Speckle 2.0";

    public override Guid Id => new Guid("074ee50d-763d-495a-8be2-6934897dd6b1");

    public override string AuthorName => "AEC Systems";

    public override string AuthorContact => "hello@speckle.systems";
  }

  public class Loader : GH_AssemblyPriority
  {

    public Loader() { }

    public override GH_LoadingInstruction PriorityLoad()
    {
      Setup.Init(Applications.Grasshopper);
      return GH_LoadingInstruction.Proceed;
    }
  }
}
