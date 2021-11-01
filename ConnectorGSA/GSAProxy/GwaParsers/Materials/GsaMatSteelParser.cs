using Speckle.GSA.API.GwaSchema;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.MAT_STEEL, GwaSetCommandType.Set, true)]
  public class GsaMatSteelParser : GwaParser<GsaMatSteel>
  {
    public GsaMatSteelParser(GsaMatSteel gsaMatSteel) : base(gsaMatSteel)  {  }

    public GsaMatSteelParser() : base(new GsaMatSteel()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      //MAT_STEEL.3 | num | <mat> | fy | fu | eps_p | Eh
      if (!AddMat(remainingItems, out remainingItems)) return false;
      return FromGwaByFuncs(remainingItems, out var _, (v) => AddNullableDoubleValue(v, out record.Fy), (v) => AddNullableDoubleValue(v, out record.Fu),
      (v) => AddNullableDoubleValue(v, out record.EpsP), (v) => AddNullableDoubleValue(v, out record.Eh));
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //MAT_STEEL.3 | num | <mat> | fy | fu | eps_p | Eh
      AddMat(ref items);
      AddItems(ref items, record.Fy, record.Fu, record.EpsP, record.Eh);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private bool AddMat(ref List<string> items)
    {
      var gsaMatParser = new GsaMatParser(record.Mat);
      if (!gsaMatParser.Gwa(out var gwa))
      {
        return false;
      }
      items.Add(gwa.First());
      return true;
    }
    #endregion

    #region from_gwa_fns
    private bool AddMat(List<string> items, out List<string> remainingItems)
    {
      record.Mat = new GsaMat();
      Join(items, out var matGwa);
      var gsaMatParser = new GsaMatParser();
      if (!gsaMatParser.FromGwa(matGwa, out remainingItems))
      {
        remainingItems = items;
        return false;
      }
      record.Mat = (GsaMat)gsaMatParser.Record;
      return true;
    }
    #endregion

  }
}
