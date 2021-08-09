using System;
using System.Collections.Generic;
using System.Linq;


namespace Speckle.GSA.API.GwaSchema
{
  [GsaType(GwaKeyword.LOAD_GRAVITY, GwaSetCommandType.SetAt, true, GwaKeyword.LOAD_TITLE, GwaKeyword.EL, GwaKeyword.MEMB, GwaKeyword.NODE)]
  public class GsaLoadGravity : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public List<int> Entities = new List<int>();
    public List<int> Nodes = new List<int>();
    public int? LoadCaseIndex;
    public double? X;
    public double? Y;
    public double? Z;

    public GsaLoadGravity()
    {
      Version = 3;
    }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      //LOAD_GRAVITY.3 | name | elemlist | nodelist | case | x | y | z
      return FromGwaByFuncs(remainingItems, out var _, AddName, (v) => AddEntities(v, out Entities), (v) => AddNodes(v, out Nodes), 
        (v) => AddNullableIndex(v, out LoadCaseIndex), (v) => AddNullableDoubleValue(v, out X), 
        (v) => AddNullableDoubleValue(v, out Y), (v) => AddNullableDoubleValue(v, out Z));
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //LOAD_GRAVITY.3 | name | elemlist | nodelist | case | x | y | z
      AddItems(ref items, Name, AddEntities(Entities), AddNodes(Nodes), LoadCaseIndex ?? 0, X ?? 0, Y ?? 0, Z ?? 0);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }
  }
}
