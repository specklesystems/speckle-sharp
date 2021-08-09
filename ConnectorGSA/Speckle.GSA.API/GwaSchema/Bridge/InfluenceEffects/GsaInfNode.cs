using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;


namespace Speckle.GSA.API.GwaSchema
{
  [GsaType(GwaKeyword.INF_NODE, GwaSetCommandType.SetAt, true, false, true, GwaKeyword.NODE, GwaKeyword.AXIS)]
  public class GsaInfNode : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public int? Action;
    public int? Node;
    public double? Factor;
    public InfType Type;
    public AxisRefType AxisRefType;
    public AxisDirection6 Direction;

    public GsaInfNode() : base()
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

      //INF_NODE | name | action | node | factor | type | axis | dir
      return FromGwaByFuncs(remainingItems, out var _, AddName, AddAction, AddNode, AddFactor, AddType, AddAxis, AddDirection);
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //INF_NODE | name | action | node | factor | type | axis | dir
      AddItems(ref items, Name, Action, Node, Factor, AddType(), AddAxis(), AddDirection());

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddType()
    {
      return Type.ToString();
    }

    private string AddAxis()
    {
      if (AxisRefType == AxisRefType.Local)
      {
        return "LOCAL";
      }
      //Assume global to be the default
      return "GLOBAL";
    }

    private string AddDirection()
    {
      return Direction.ToString();
    }
    #endregion

    #region from_gwa_fns
    private bool AddAction(string v)
    {
      Action = (int.TryParse(v, out var value) && value > 0) ? (int?)value : null;
      return true;
    }

    private bool AddNode(string v)
    {
      Node = (int.TryParse(v, out var value) && value > 0) ? (int?)value : null;
      return true;
    }

    private bool AddFactor(string v)
    {
      Factor = (double.TryParse(v, out var value) && value != 0) ? (double?)value : null;
      return true;
    }

    private bool AddType(string v)
    {
      Type = Enum.TryParse<InfType>(v, true, out var t) ? t : InfType.NotSet;
      return true;
    }

    private bool AddAxis(string v)
    {
      AxisRefType = Enum.TryParse<AxisRefType>(v, true, out var refType) ? refType : AxisRefType.NotSet;
      return true;
    }

    private bool AddDirection(string v)
    {
      Direction = Enum.TryParse<AxisDirection6>(v, true, out var dir) ? dir : AxisDirection6.NotSet;
      return true;
    }
    #endregion
  }
}
