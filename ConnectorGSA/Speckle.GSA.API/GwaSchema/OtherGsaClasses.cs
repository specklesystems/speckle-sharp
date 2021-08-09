using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Speckle.GSA.API.GwaSchema
{
  public abstract class BlankGsaRecord : GsaRecord
  {
    public override bool FromGwa(string gwa) => false;
    public override bool Gwa(out List<string> gwa, bool includeSet = false) { throw new NotImplementedException(); }
  }

  //This seems to be an alternative to SECTION (corresponding to 1D properties) - to be investigated further
  [GsaType(GwaKeyword.PROP_SEC, GwaSetCommandType.Set, true, GwaKeyword.MAT_STEEL, GwaKeyword.MAT_CONCRETE)]
  public class GsaPropSec : BlankGsaRecord { }
}
