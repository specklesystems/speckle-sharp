using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.TOL, GwaSetCommandType.SetNoIndex, true)]
  public class GwaTolParser : GwaParser<GsaTol>
  {
    public GwaTolParser(GsaTol gsaTol) : base(gsaTol) { }

    public GwaTolParser() : base(new GsaTol()) { }

    public override bool FromGwa(string gwa)
    {
      var items = Split(gwa);
      var remainingItems = new List<string>();
      if (items.Count() == 0)
      {
        return false;
      }

      gwaSetCommandType = GwaSetCommandType.SetNoIndex;

      if (items[0].StartsWith("set", StringComparison.InvariantCultureIgnoreCase))
      {
        items.Remove(items[0]);
      }

      if (ParseKeywordVersionSid(items[0]))
      {
        //Remove keyword
        items.Remove(items[0]);
      }
      else
      {
        return false;
      }

      //TOL | vertical | node | grid_edge | grid_angle | spacer_leg | memb_angle | memb_edge | refinement | elem_plane | member_curve
      if (FromGwaByFuncs(items, out _, AddVertical, (v) => AddRoundedDecimal(v, out record.Node), 
        (v) => AddRoundedDecimal(v, out record.GridEdge), (v) => AddRoundedDecimal(v, out record.GridAngle),
        (v) => AddRoundedDecimal(v, out record.SpacerLeg), (v) => AddRoundedDecimal(v, out record.MembAngle),
        (v) => AddRoundedDecimal(v, out record.MembEdge), (v) => AddRoundedDecimal(v, out record.Refinement),
        AddElemPlane, (v) => AddRoundedDecimal(v, out record.MemberCurve)))

      {
        //SPEC_CONC_DESIGN records have no index or name field, but there should only be one such record the whole model, so the application ID
        //can reasonably in practice be assigned to be equal to the keyword
        record.ApplicationId = GwaKeyword.SPEC_CONC_DESIGN.GetStringValue();
        return true;
      }
      return false;
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      var items = new List<string>();
      if (includeSet)
      {
        items.Add("SET");
      }
      items.Add(keyword + "." + record.Version);

      //TOL | vertical | node | grid_edge | grid_angle | spacer_leg | memb_angle | memb_edge | refinement | elem_plane | member_curve
      AddItems(ref items, Math.Tan(record.VerticalDegrees), record.Node, record.GridEdge, record.GridAngle, record.SpacerLeg,
        record.MembAngle, record.Refinement, Math.Tan(record.ElemPlaneDegrees), record.MemberCurve);

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region to_gwa_fns

    #endregion

    #region from_gwa_fns
    public bool AddRoundedDecimal(string v, out double recordValue)
    {
      if (double.TryParse(v, out double tempValue))
      {
        recordValue = Math.Round(tempValue, 8);
        return true;
      }
      recordValue = 0;
      return false;
    }

    public bool AddVertical(string v)
    {
      if (double.TryParse(v, out double tanDegrees))
      {
        record.VerticalDegrees = Math.Atan(tanDegrees);
        return true;
      }
      return false;
    }

    public bool AddElemPlane(string v)
    {
      if (double.TryParse(v, out double tanDegrees))
      {
        record.ElemPlaneDegrees = Math.Atan(tanDegrees);
        return true;
      }
      return false;
    }
    #endregion
  }
}
