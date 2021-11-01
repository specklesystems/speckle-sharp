using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.INF_BEAM, GwaSetCommandType.SetAt, true, false, true, GwaKeyword.AXIS, GwaKeyword.EL)]
  public class GsaInfBeamParser : GwaParser<GsaInfBeam>
  {
    public GsaInfBeamParser(GsaInfBeam gsaInfBeam) : base(gsaInfBeam) { }

    public GsaInfBeamParser() : base(new GsaInfBeam()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      //Based on an example taken from a GSA 10.1 model. Documentation does not have version 2
      //INF_BEAM.2 | name | action | elem | pos | factor | type | dir
      return FromGwaByFuncs(remainingItems, out var _, AddName, AddAction, AddElement, AddPosition, AddFactor, AddType, AddDirection);
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //INF_BEAM.2 | name | action | elem | pos | factor | type | dir
      AddItems(ref items, record.Name, record.Index, record.Element, AddPosition(), record.Factor, AddType(), AddDirection());

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddPosition()
    {
      return (record.Position * 100).ToString() + "%";
    }

    private string AddType()
    {
      return record.Type.ToString();
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

    private bool AddElement(string v)
    {
      record.Element = (int.TryParse(v, out var value) && value > 0) ? (int?)value : null;
      return true;
    }

    private bool AddPosition(string v)
    {
      record.Position = double.Parse(v.TrimEnd(new char[] { '%', ' ' })) / 100;
      //Position = (double.TryParse(v, out var value) && value != 0) ? (double?)value : null;
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

    private bool AddDirection(string v)
    {
      record.Direction = Enum.TryParse<AxisDirection6>(v, true, out var dir) ? dir : AxisDirection6.NotSet;
      return true;
    }
    #endregion
  }
}
