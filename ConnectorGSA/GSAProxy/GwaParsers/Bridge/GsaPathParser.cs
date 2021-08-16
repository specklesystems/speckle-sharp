using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.PATH, GwaSetCommandType.Set, true, GwaKeyword.ALIGN)]
  public class GsaPathParser : GwaParser<GsaPath>
  {
    public GsaPathParser(GsaPath gsaPath) : base(gsaPath) { }

    public GsaPathParser() : base(new GsaPath()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      //PATH | num | name | type | group | alignment | left | right | factor | num_marked_lanes
      return FromGwaByFuncs(remainingItems, out var _, AddName, AddType, AddGroup, AddAlignment, AddLeft, AddRight, AddFactor, AddNumMarkedLanes);
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //PATH | num | name | type | group | alignment | left | right | factor | num_marked_lanes
      AddItems(ref items, record.Name, AddType(), record.Group, record.Alignment, record.Left, record.Right, record.Factor, record.NumMarkedLanes);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddType()
    {
      return record.Type.ToString();
    }
    #endregion

    #region from_gwa_fns
    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    private bool AddType(string v)
    {
      record.Type = Enum.TryParse<PathType>(v, true, out var t) ? t : PathType.NotSet;
      return true;
    }
    private bool AddGroup(string v)
    {
      record.Group = (int.TryParse(v, out var value) && value > 0) ? (int?)value : null;
      return true;
    }

    private bool AddAlignment(string v)
    {
      record.Alignment = (int.TryParse(v, out var value) && value > 0) ? (int?)value : null;
      return true;
    }

    private bool AddLeft(string v)
    {
      record.Left = (double.TryParse(v, out var value) && value != 0) ? (double?)value : null;
      return true;
    }

    private bool AddRight(string v)
    {
      record.Right = (double.TryParse(v, out var value) && value != 0) ? (double?)value : null;
      return true;
    }

    private bool AddFactor(string v)
    {
      record.Factor = (double.TryParse(v, out var value) && value != 0) ? (double?)value : null;
      return true;
    }

    private bool AddNumMarkedLanes(string v)
    {
      record.NumMarkedLanes = (int.TryParse(v, out var value) && value >= 0) ? (int?)value : null;
      return true;
    }
    #endregion
  }
}
