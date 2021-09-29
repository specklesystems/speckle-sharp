using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.MAT_CURVE, GwaSetCommandType.Set, true)]
  public class GsaMatCurveParser : GwaParser<GsaMatCurve>
  {
    public GsaMatCurveParser(GsaMatCurve gsaMatCurve) : base(gsaMatCurve) { }

    public GsaMatCurveParser() : base(new GsaMatCurve()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      //MAT_CURVE | num | name | abs | ord | table
      return FromGwaByFuncs(remainingItems, out var _, AddName, AddAbscissa, AddOrdinate, AddTable);
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //MAT_CURVE | num | name | abs | ord | table
      AddItems(ref items, record.Name, record.Abscissa.ToString(), record.Ordinate.ToString(), AddTable());

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddTable()
    {
      var str = "\"";
      for (var i = 0; i < record.Table.GetLength(0); i++)
      {
        str += "(" + record.Table[i, 0].ToString() + "," + record.Table[i, 1].ToString() + ") ";
      }
      return str.Remove(str.Length - 1) + "\"";
    }
    #endregion

    #region from_gwa_fns
    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    private bool AddAbscissa(string v)
    {
      if (Enum.TryParse<Dimension>(v, true, out var value))
      {
        record.Abscissa = value;
        return true;
      }
      return false;
    }

    private bool AddOrdinate(string v)
    {
      if (Enum.TryParse<Dimension>(v, true, out var value))
      {
        record.Ordinate = value;
        return true;
      }
      return false;
    }

    private bool AddTable(string v)
    {
      var rows = v.Trim('"').Replace("(", "").Replace(")", "").Split(' ');
      record.Table = new double[rows.Length, 2];
      for (var i = 0; i < rows.Length; i++)
      {
        var data = rows[i].Split(',');
        record.Table[i, 0] = data[0].ToDouble();
        record.Table[i, 1] = data[1].ToDouble();
      }
      return true;
    }
    #endregion
  }
}
