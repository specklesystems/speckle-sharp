using System.Collections.Generic;

namespace Speckle.GSA.API.GwaSchema
{
  public abstract class GsaSectionComponentBase : GsaRecord
  {
    //This is for embedding into SECTION records - returning the unjoined string arguments so that the SECTION
    //Gwa code can join it
    public abstract bool GwaItems(out List<string> items, bool includeSet = false, bool includeRef = false);
  }
}
