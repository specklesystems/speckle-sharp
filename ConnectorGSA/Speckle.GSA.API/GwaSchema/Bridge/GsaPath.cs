using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;


namespace Speckle.GSA.API.GwaSchema
{
  [GsaType(GwaKeyword.PATH, GwaSetCommandType.Set, true, GwaKeyword.ALIGN)]
  public class GsaPath : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public PathType Type;
    public int? Group;
    public int? Alignment;
    public double? Left;
    public double? Right;
    public double? Factor;
    public int? NumMarkedLanes;

    public GsaPath() : base()
    {
      //Defaults
      Version = 1;
    }

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
      AddItems(ref items, Name, AddType(), Group, Alignment, Left, Right, Factor, NumMarkedLanes);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddType()
    {
      return Type.ToString();
    }
    #endregion

    #region from_gwa_fns
    private bool AddType(string v)
    {
      Type = Enum.TryParse<PathType>(v, true, out var t) ? t : PathType.NotSet;
      return true;
    }
    private bool AddGroup(string v)
    {
      Group = (int.TryParse(v, out var value) && value > 0) ? (int?)value : null;
      return true;
    }

    private bool AddAlignment(string v)
    {
      Alignment = (int.TryParse(v, out var value) && value > 0) ? (int?)value : null;
      return true;
    }

    private bool AddLeft(string v)
    {
      Left = (double.TryParse(v, out var value) && value != 0) ? (double?)value : null;
      return true;
    }

    private bool AddRight(string v)
    {
      Right = (double.TryParse(v, out var value) && value != 0) ? (double?)value : null;
      return true;
    }

    private bool AddFactor(string v)
    {
      Factor = (double.TryParse(v, out var value) && value != 0) ? (double?)value : null;
      return true;
    }

    private bool AddNumMarkedLanes(string v)
    {
      NumMarkedLanes = (int.TryParse(v, out var value) && value >= 0) ? (int?)value : null;
      return true;
    }
    #endregion
  }
}
