using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;


namespace Speckle.GSA.API.GwaSchema
{
  [GsaType(GwaKeyword.MAT_CURVE, GwaSetCommandType.Set, true)]
  public class GsaMatCurve : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public Dimension Abscissa;
    public Dimension Ordinate;
    public double [,] Table;

    public GsaMatCurve() : base()
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
      AddItems(ref items, Name, Abscissa.ToString(), Ordinate.ToString(), AddTable());

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddTable()
    {
      var str = "\"";
      for (var i = 0; i < Table.GetLength(0); i++)
      {
        str += "(" + Table[i, 0].ToString() + "," + Table[i, 1].ToString() + ") ";
      }
      return str.Remove(str.Length - 1) + "\"";
    }
    #endregion

    #region from_gwa_fns
    private bool AddAbscissa(string v)
    {
      if (Enum.TryParse<Dimension>(v, true, out var value))
      {
        Abscissa = value;
        return true;
      }
      return false;
    }

    private bool AddOrdinate(string v)
    {
      if (Enum.TryParse<Dimension>(v, true, out var value))
      {
        Ordinate = value;
        return true;
      }
      return false;
    }

    private bool AddTable(string v)
    {
      var rows = v.Trim('"').Replace("(","").Replace(")","").Split(' ');
      Table = new double[rows.Length, 2];
      for(var i = 0; i < rows.Length; i++)
      {
        var data = rows[i].Split(',');
        Table[i, 0] = data[0].ToDouble();
        Table[i, 1] = data[1].ToDouble();
      }
      return true;
    }
    #endregion
  }
}
