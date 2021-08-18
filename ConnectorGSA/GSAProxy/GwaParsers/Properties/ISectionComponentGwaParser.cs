using System.Collections.Generic;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  public interface ISectionComponentGwaParser
  {
    bool GwaItems(out List<string> items, bool includeSet = false, bool includeRef = false);
  }
}
