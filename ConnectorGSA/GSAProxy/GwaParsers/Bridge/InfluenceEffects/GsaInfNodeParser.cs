using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.INF_NODE, GwaSetCommandType.SetAt, true, false, true, GwaKeyword.NODE, GwaKeyword.AXIS)]
  public class GsaInfNodeParser : GwaParser<GsaInfNode>
  {
    public GsaInfNodeParser(GsaInfNode gsaInfNode) : base(gsaInfNode) { }

    public GsaInfNodeParser() : base(new GsaInfNode()) { }

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
      AddItems(ref items, record.Name, record.Index, record.Node, record.Factor, AddType(), AddAxis(), AddDirection());

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddType()
    {
      return record.Type.ToString();
    }

    private string AddAxis()
    {
      if (record.AxisRefType == AxisRefType.Local)
      {
        return "LOCAL";
      }
      //Assume global to be the default
      return "GLOBAL";
    }

    private string AddDirection()
    {
      return record.Direction.ToString();
    }
    #endregion

    #region from_gwa_fns
    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    private bool AddAction(string v)
    {
      record.Index = (int.TryParse(v, out var value) && value > 0) ? (int?)value : null;
      return true;
    }

    private bool AddNode(string v)
    {
      record.Node = (int.TryParse(v, out var value) && value > 0) ? (int?)value : null;
      return true;
    }

    private bool AddFactor(string v)
    {
      record.Factor = (double.TryParse(v, out var value) && value != 0) ? (double?)value : null;
      return true;
    }

    private bool AddType(string v)
    {
      record.Type = Enum.TryParse<InfType>(v, true, out var t) ? t : InfType.NotSet;
      return true;
    }

    private bool AddAxis(string v)
    {
      //record.AxisRefType = Enum.TryParse<AxisRefType>(v, true, out var refType) ? refType : AxisRefType.NotSet;
      //return true;

      if (v.Equals("global", StringComparison.InvariantCultureIgnoreCase))
      {
        record.AxisRefType = AxisRefType.Global;
      }
      else if (v.Equals("local", StringComparison.InvariantCultureIgnoreCase))
      {
        record.AxisRefType = AxisRefType.Local;
      }
      else if (int.TryParse(v, out var foundIndex) && foundIndex > 0)
      {
        record.AxisRefType = AxisRefType.Reference;
        record.AxisIndex = foundIndex;
      }
      else
      {
        record.AxisRefType = AxisRefType.NotSet;
      }
      return true;
    }

    private bool AddDirection(string v)
    {
      record.Direction = Enum.TryParse<AxisDirection6>(v, true, out var dir) ? dir : AxisDirection6.NotSet;
      return true;
    }
    #endregion
  }
}
