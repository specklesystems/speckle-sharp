using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.LOAD_GRAVITY, GwaSetCommandType.SetAt, true, GwaKeyword.LOAD_TITLE, GwaKeyword.EL, GwaKeyword.MEMB, GwaKeyword.NODE)]
  public class GsaLoadGravityParser : GwaParser<GsaLoadGravity>
  {
    public GsaLoadGravityParser(GsaLoadGravity gsaLoadGravity) : base(gsaLoadGravity) { }

    public GsaLoadGravityParser() : base(new GsaLoadGravity()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      //LOAD_GRAVITY.3 | name | elemlist | nodelist | case | x | y | z
      return FromGwaByFuncs(remainingItems, out var _, AddName, (v) => AddEntities(v, out record.MemberIndices, out record.ElementIndices), (v) => AddNodes(v, out record.Nodes), 
        (v) => AddNullableIndex(v, out record.LoadCaseIndex), (v) => AddNullableDoubleValue(v, out record.X), 
        (v) => AddNullableDoubleValue(v, out record.Y), (v) => AddNullableDoubleValue(v, out record.Z));
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //LOAD_GRAVITY.3 | name | elemlist | nodelist | case | x | y | z
      AddItems(ref items, record.Name, AddEntities(record.MemberIndices, record.ElementIndices), AddNodes(record.Nodes), record.LoadCaseIndex ?? 0, record.X ?? 0, record.Y ?? 0, record.Z ?? 0);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region from_gwa_fns
    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }
    #endregion
  }
}
