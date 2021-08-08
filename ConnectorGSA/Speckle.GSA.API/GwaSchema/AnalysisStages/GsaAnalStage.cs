using System;
using System.Collections.Generic;
using System.Linq;


namespace Speckle.GSA.API.GwaSchema
{
  [GsaType(GwaKeyword.ANAL_STAGE, GwaSetCommandType.Set, true)]
  public class GsaAnalStage : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public Colour Colour;
    public List<int> List;
    public double? Phi;
    public int? Days;
    public List<int> Lock;

    public GsaAnalStage()
    {
      Version = 3;
    }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      //ANAL_STAGE.3 | stage | name | colour | list | phi | time | lock
      return FromGwaByFuncs(remainingItems, out var _, AddName, (v) => AddColour(v, out Colour), (v) => AddEntities(v, out List), 
        (v) => AddNullableDoubleValue(v, out Phi), (v) => AddNullableIntValue(v, out Days), (v) => AddEntities(v, out Lock));
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //ANAL_STAGE.3 | stage | name | colour | list | phi | time | lock
      AddItems(ref items, Name, Colour.NO_RGB.ToString(), AddEntities(List), Phi ?? 0, Days ?? 0, AddEntities(Lock));

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns

    #endregion

    #region from_gwa_fns


    #endregion
  }
}
