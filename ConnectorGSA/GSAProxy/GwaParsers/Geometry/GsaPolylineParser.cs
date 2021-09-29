using Interop.Gsa_10_1;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.POLYLINE, GwaSetCommandType.Set, true, true, true, GwaKeyword.GRID_PLANE)]

  public class GsaPolylineParser : GwaParser<GsaPolyline>
  {
    public GsaPolylineParser(GsaPolyline gsaPolyline) : base(gsaPolyline) { }
    public GsaPolylineParser() : base(new GsaPolyline()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      //Constructed from using 10.1 rather than consulting docs at https://www.oasys-software.com/help/gsa/10.1/GSA_Text.html
      //which, at the time of writing, only reports up to version 3
      //POLYLINE | num | name | colour | grid_plane | num_dim | desc
      return FromGwaByFuncs(remainingItems, out var _, AddName, (v) => AddColour(v, out record.Colour), 
        (v) => AddNullableIntValue(v, out record.GridPlaneIndex), (v) => int.TryParse(v, out record.NumDim), ProcDesc);
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //POLYLINE | num | name | colour | grid_plane | num_dim | desc
      AddItems(ref items, record.Name, Colour.NO_RGB.ToString(), (record.GridPlaneIndex ?? 0), 
        record.NumDim, AddDesc());

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddDesc()
    {
      var desc = new List<string>();
      var v = record.Values;
      for (int i = 0; i < v.Count; i += record.NumDim)
      {
        var point = "(" + v[i] + "," + v[i + 1];
        if (record.NumDim == 3) point += "," + v[i + 2];
        point += ")";
        desc.Add(point); 
      }
      desc.Add("(" + record.Units + ")");
      return string.Join(" ", desc.ToArray());
    }
    #endregion

    #region from_gwa_fns
    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }
    protected bool ProcDesc(string v)
    {
      //coordinates
      var coords = new List<double>();
      foreach (var item in v.Split(' '))
      {
        var point = item.Split('(', ')')[1].Split(',').Select(c => c.ToDouble()).ToList();
        coords.AddRange(point);
      }
      record.Values = coords;

      //units
      record.Units = v.Split(' ').Last().Split('(', ')').Last();

      return true;
    }
    #endregion
  }
}
