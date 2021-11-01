using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.ALIGN, GwaSetCommandType.Set, true, GwaKeyword.GRID_SURFACE)]
  public class GsaAlignParser : GwaParser<GsaAlign>
  {

    public GsaAlignParser(GsaAlign gsaAlign) : base(gsaAlign) { }

    public GsaAlignParser() : base(new GsaAlign()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      var items = remainingItems;
      //ALIGN | num | name | surface | num ( | chain | curv )
      if (!FromGwaByFuncs(items, out remainingItems, AddName, AddSurface, AddNumAlignmentPoints))
      {
        return false;
      }

      //Loop through remaining items and add pairs of points to Chain and Curv lists
      record.Chain = new List<double>();
      record.Curv = new List<double>();
      for (var i = 0; i < record.NumAlignmentPoints; i++)
      {
        items = remainingItems;
        if (!FromGwaByFuncs(items, out remainingItems, AddChain, AddCurv))
        {
          return false;
        }
      }
      return true;
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //ALIGN | num | name | surface | num ( | chain | curv )
      AddItems(ref items, record.Name, record.GridSurfaceIndex, record.NumAlignmentPoints);

      int alignmentPoints = 0;
      if (record.NumAlignmentPoints.HasValue)
      {
        alignmentPoints = Math.Min((int)Math.Min(record.Chain.Count(), record.Curv.Count()), record.NumAlignmentPoints.Value);
      }
      else if (record.Chain.Any() && record.Curv.Any())
      {
        alignmentPoints = Math.Min(record.Chain.Count(), record.Curv.Count());
      }
      for (var i = 0; i < alignmentPoints; i++)
      {
        items.Add(record.Chain[i].ToString());
        items.Add(record.Curv[i].ToString());
      }

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

    private bool AddSurface(string v)
    {
      record.GridSurfaceIndex = (int.TryParse(v, out var surfaceIndex) && surfaceIndex > 0) ? (int?)surfaceIndex : null;
      return true;
    }
    private bool AddNumAlignmentPoints(string v)
    {
      record.NumAlignmentPoints = (int.TryParse(v, out var value) && value > 0) ? (int?)value : null;
      return true;
    }

    private bool AddChain(string v)
    {
      if (double.TryParse(v, out var value) && value >= 0)
      {
        record.Chain.Add(value);
        return true;
      }
      return false;
    }

    private bool AddCurv(string v)
    {
      if (double.TryParse(v, out var value))
      {
        record.Curv.Add(value);
        return true;
      }
      return false;
    }
    #endregion
  }
}
