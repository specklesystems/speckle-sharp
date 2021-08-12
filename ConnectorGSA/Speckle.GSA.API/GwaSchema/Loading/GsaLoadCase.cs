using System.Collections.Generic;
using System.Linq;

namespace Speckle.GSA.API.GwaSchema
{
  public class GsaLoadCase : GsaRecord_
  {
    public StructuralLoadCaseType CaseType;
    public string Title;

    public GsaLoadCase() : base()
    {
      //Defaults
      Version = 2;
    }

  }
}
