using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;


namespace Speckle.GSA.API.GwaSchema
{
  [GsaType(GwaKeyword.MAT, GwaSetCommandType.Set, false, false, true)]
  public class GsaMat : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public double? E;
    public double? F;
    public double? Nu;
    public double? G;
    public double? Rho;
    public double? Alpha;
    public GsaMatAnal Prop;
    public int NumUC;
    public Dimension AbsUC;
    public Dimension OrdUC;
    public double[] PtsUC;
    public int NumSC;
    public Dimension AbsSC;
    public Dimension OrdSC;
    public double[] PtsSC;
    public int NumUT;
    public Dimension AbsUT;
    public Dimension OrdUT;
    public double[] PtsUT;
    public int NumST;
    public Dimension AbsST;
    public Dimension OrdST;
    public double[] PtsST;
    public double? Eps;
    public GsaMatCurveParam Uls;
    public GsaMatCurveParam Sls;
    public double? Cost;
    public MatType Type;

    public GsaMat() : base()
    {
      //Defaults
      Version = 10;
    }

    public override bool FromGwa(string gwa)
    {
      return FromGwa(gwa, out var _);
    }

    public bool FromGwa(string gwa, out List<string> remainingItems)
    {
      //Process the first part of gwa string
      remainingItems = Split(gwa);
      if (remainingItems[0].StartsWith("set", StringComparison.OrdinalIgnoreCase))
      {
        remainingItems.Remove(remainingItems[0]);
      }
      if (!ParseKeywordVersionSid(remainingItems[0]))
      {
        return false;
      }
      remainingItems = remainingItems.Skip(1).ToList();

      //Detect presence or absense of num (record index) argument based on number of items
      if (int.TryParse(remainingItems[0], out var foundIndex))
      {
        Index = foundIndex;
        remainingItems = remainingItems.Skip(1).ToList();
      }

      //MAT.10 | num | name | E | f | nu | G | rho | alpha | <prop> | num_uc (| abs | ord | pts[] |) num_sc (| abs | ord | pts[] |) num_ut (| abs | ord | pts[] |) num_st (| abs | ord | pts[] |) eps | <uls> | <sls> | cost | type
      if (!FromGwaByFuncs(remainingItems, out remainingItems, AddName, (v) => AddNullableDoubleValue(v, out E), (v) => AddNullableDoubleValue(v, out F),
        (v) => AddNullableDoubleValue(v, out Nu), (v) => AddNullableDoubleValue(v, out G), (v) => AddNullableDoubleValue(v, out Rho),
        (v) => AddNullableDoubleValue(v, out Alpha)))
      {
        return false;
      }

      //Process MAT_ANAL keyword
      if (!AddMatAnal(remainingItems, out remainingItems)) return false;

      //Process Explicit curves
      if (!AddExplicitCurve(remainingItems, out remainingItems, out NumUC, out AbsUC, out OrdUC, out PtsUC)) return false;
      if (!AddExplicitCurve(remainingItems, out remainingItems, out NumSC, out AbsSC, out OrdSC, out PtsSC)) return false;
      if (!AddExplicitCurve(remainingItems, out remainingItems, out NumUT, out AbsUT, out OrdUT, out PtsUT)) return false;
      if (!AddExplicitCurve(remainingItems, out remainingItems, out NumST, out AbsST, out OrdST, out PtsST)) return false;

      if (!FromGwaByFuncs(remainingItems, out remainingItems, (v) => AddNullableDoubleValue(v, out Eps)))  return false;

        //Process uls / sls curves
      if (!AddMatCurveParam(remainingItems, out remainingItems, out Uls)) return false;
      if (!AddMatCurveParam(remainingItems, out remainingItems, out Sls)) return false;

      //Process final items
      return FromGwaByFuncs(remainingItems, out remainingItems, (v) => AddNullableDoubleValue(v, out Cost), (v) => Enum.TryParse<MatType>(v, true, out Type));
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }
      if (Index == null) //embedded
      {
        items.RemoveAt(items.Count - 1);
      }
      //MAT.10 | num | name | E | f | nu | G | rho | alpha | <prop> | num_uc (| abs | ord | pts[] |) num_sc (| abs | ord | pts[] |) num_ut (| abs | ord | pts[] |) num_st (| abs | ord | pts[] |) eps | <uls> | <sls> | cost | type
      AddItems(ref items, Name, E, F, Nu, G, Rho, Alpha); //Add items up until <prop>
      AddObject(ref items, Prop); //Add GsaMatAnal object
      AddExplicitCurves(ref items); //Add items associated with the explicit curves
      AddItems(ref items, Eps);
      AddObject(ref items, Uls); //Add GsaMatCurveParam object
      AddObject(ref items, Sls); //Add GsaMatCurveParam object
      AddItems(ref items, Cost, Type);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private bool AddObject(ref List<string> items, GsaMatAnal x)
    {
      if (!x.Gwa(out var gwa))
      {
        return false;
      }
      items.Add(gwa.First());
      return true;
    }

    private bool AddObject(ref List<string> items, GsaMatCurveParam x)
    {
      if (!x.Gwa(out var gwa))
      {
        return false;
      }
      items.Add(gwa.First());
      return true;
    }

    private bool AddExplicitCurves(ref List<string> items)
    {
      //num_uc(| abs | ord | pts[] |) num_sc(| abs | ord | pts[] |) num_ut(| abs | ord | pts[] |) num_st(| abs | ord | pts[] |)
      //ULS - Compression
      if (NumUC > 0)
      {
        AddItems(ref items, NumUC, AbsUC, OrdUC, PtsUC);
      }
      else
      {
        AddItems(ref items, NumUC);
      }

      //SLS - Compression
      if (NumSC > 0)
      {
        AddItems(ref items, NumSC, AbsSC, OrdSC, PtsSC);
      }
      else
      {
        AddItems(ref items, NumSC);
      }

      //ULS - tension
      if (NumUT > 0)
      {
        AddItems(ref items, NumUT, AbsUT, OrdUT, PtsUT);
      }
      else
      {
        AddItems(ref items, NumUT);
      }

      //SLS - tension
      if (NumST > 0)
      {
        AddItems(ref items, NumST, AbsST, OrdST, PtsST);
      }
      else
      {
        AddItems(ref items, NumST);
      }
      return true;
    }
    #endregion

    #region from_gwa_fns
    private bool AddMatAnal(List<string> items, out List<string> remainingItems)
    {
      Prop = new GsaMatAnal();
      Join(items, out var matAnalGwa);
      if (!Prop.FromGwa(matAnalGwa, out remainingItems))
      {
        remainingItems = items;
        return false;
      }
      return true;
    }

    private bool AddExplicitCurve(List<string> items, out List<string> remainingItems, out int Num, out Dimension Abs, out Dimension Ord, out double[] Pts)
    {
      try
      {
        int.TryParse(items[0], out Num);
        Pts = new double[Num];
        if (Num > 0)
        {
          Enum.TryParse<Dimension>(items[1], true, out Abs);
          Enum.TryParse<Dimension>(items[2], true, out Ord);
          for (int i = 0; i < Num; i++)
          {
            double.TryParse(items[i + 3], out Pts[i]);
          }
          remainingItems = items.Skip(3 + Num).ToList();
        }
        else
        {
          Abs = Dimension.NotSet;
          Ord = Dimension.NotSet;
          remainingItems = items.Skip(1).ToList();
        }
        return true;
      }
      catch
      {
        Num = 0;
        Abs = Dimension.NotSet;
        Ord = Dimension.NotSet;
        Pts = new double[Num];
        remainingItems = items;
        return false;
      }
    }

    private bool AddMatCurveParam(List<string> items, out List<string> remainingItems, out GsaMatCurveParam curve)
    {
      curve = new GsaMatCurveParam();
      Join(items, out var gwa);
      if (!curve.FromGwa(gwa, out remainingItems))
      {
        remainingItems = items;
        return false;
      }
      return true;
    }
    #endregion
  }
}
