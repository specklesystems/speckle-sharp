using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;


namespace Speckle.GSA.API.GwaSchema
{
  [GsaType(GwaKeyword.MAT_STEEL, GwaSetCommandType.Set, true)]
  public class GsaMatSteel : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public GsaMat Mat;
    public double? Fy;
    public double? Fu;
    public double? EpsP;
    public double? Eh;

    public GsaMatSteel() : base()
    {
      //Defaults
      Version = 3;
    }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      //MAT_STEEL.3 | num | <mat> | fy | fu | eps_p | Eh
      if (!AddMat(remainingItems, out remainingItems)) return false;
      return FromGwaByFuncs(remainingItems, out var _, (v) => AddNullableDoubleValue(v, out Fy), (v) => AddNullableDoubleValue(v, out Fu),
      (v) => AddNullableDoubleValue(v, out EpsP), (v) => AddNullableDoubleValue(v, out Eh));
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
      AddItems(ref items, Fy, Fu, EpsP, Eh);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private bool AddMat(ref List<string> items)
    {
      if (!Mat.Gwa(out var gwa))
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
      Mat = new GsaMat();
      Join(items, out var matGwa);
      if (!Mat.FromGwa(matGwa, out remainingItems))
      {
        remainingItems = items;
        return false;
      }
      return true;
    }
    #endregion
  }
}

