using System;
using StructuralUtilities.PolygonMesher;

namespace ConnectorTeklaStructuresShared
{
  [Obsolete("See comment within implementation of UnusedMethod", true)]
  internal class PolygonMesherReferencer
  {
    [Obsolete("See comment within implementation", true)]
    public PolygonMesher UnusedMethod()
    {
      // This class is only here to throw an error if the polygon mesher dependency is ever removed
      // The dependency is needed for the converter, however the assembly resolve doesn't look in the kits
      // folder for missing dlls, it looks in the connector folder. We should probably figure out a different
      // method for dealing with converter dependencies
      return null;
    }
  }
}
