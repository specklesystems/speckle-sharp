using Speckle.GSA.API.GwaSchema;
using System.Collections.Generic;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  public abstract class GsaSectionComponentBaseParser : GwaParser<GsaSectionComponentBase>
  {
    public GsaSectionComponentBaseParser(GsaSectionComponentBase gsaSectionComponentBase) : base(gsaSectionComponentBase) { }

    //This is for embedding into SECTION records - returning the unjoined string arguments so that the SECTION
    //Gwa code can join it
    public abstract bool GwaItems(out List<string> items, bool includeSet = false, bool includeRef = false);
  }
}
