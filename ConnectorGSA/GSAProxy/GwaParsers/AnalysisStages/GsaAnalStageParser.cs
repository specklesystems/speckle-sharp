using Speckle.GSA.API.GwaSchema;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.ANAL_STAGE, GwaSetCommandType.Set, true)]
  public class GsaAnalStageParser : GwaParser<GsaAnalStage>
  {
    public GsaAnalStageParser(GsaAnalStage gsaAnalStage) : base(gsaAnalStage) { }

    public GsaAnalStageParser() : base(new GsaAnalStage()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      //ANAL_STAGE.3 | stage | name | colour | list | phi | time | lock
      return FromGwaByFuncs(remainingItems, out var _, AddName, (v) => AddColour(v, out record.Colour), (v) => AddEntities(v, out record.MemberIndices, out record.ElementIndices), 
        (v) => AddNullableDoubleValue(v, out record.Phi), (v) => AddNullableIntValue(v, out record.Days), (v) => AddEntities(v, out record.LockMemberIndices, out record.LockElementIndices));
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //ANAL_STAGE.3 | stage | name | colour | list | phi | time | lock
      AddItems(ref items, record.Name, Colour.NO_RGB.ToString(), AddEntities(record.MemberIndices, record.ElementIndices), record.Phi ?? 0, record.Days ?? 0, 
        AddEntities(record.LockMemberIndices, record.LockElementIndices));

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns

    #endregion

    #region from_gwa_fns
    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    #endregion
  }
}
