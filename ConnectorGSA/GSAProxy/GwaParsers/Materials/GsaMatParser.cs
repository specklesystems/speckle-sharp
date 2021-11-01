using Speckle.ConnectorGSA.Proxy.GwaParsers;
using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.MAT, GwaSetCommandType.Set, false, false, true)]
  public class GsaMatParser : GwaParser<GsaMat>
  {
    public GsaMatParser(GsaMat gsaMat) : base(gsaMat) { }

    public GsaMatParser() : base(new GsaMat()) { }

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
        record.Index = foundIndex;
        remainingItems = remainingItems.Skip(1).ToList();
      }

      //MAT.11 | num | name | E | f | nu | G | rho | alpha | <prop> | num_uc (| abs | ord | pts[] |) num_sc (| abs | ord | pts[] |) num_ut (| abs | ord | pts[] |) num_st (| abs | ord | pts[] |) eps | <uls> | <sls> | cost | type | env  | env_param
      if (!FromGwaByFuncs(remainingItems, out remainingItems, AddName, (v) => AddNullableDoubleValue(v, out record.E), (v) => AddNullableDoubleValue(v, out record.F),
        (v) => AddNullableDoubleValue(v, out record.Nu), (v) => AddNullableDoubleValue(v, out record.G), (v) => AddNullableDoubleValue(v, out record.Rho),
        (v) => AddNullableDoubleValue(v, out record.Alpha)))
      {
        return false;
      }

      //Process MAT_ANAL keyword
      if (!AddMatAnal(remainingItems, out remainingItems)) return false;

      //Process Explicit curves
      if (!AddExplicitCurve(remainingItems, out remainingItems, out record.NumUC, out record.AbsUC, out record.OrdUC, out record.PtsUC)) return false;
      if (!AddExplicitCurve(remainingItems, out remainingItems, out record.NumSC, out record.AbsSC, out record.OrdSC, out record.PtsSC)) return false;
      if (!AddExplicitCurve(remainingItems, out remainingItems, out record.NumUT, out record.AbsUT, out record.OrdUT, out record.PtsUT)) return false;
      if (!AddExplicitCurve(remainingItems, out remainingItems, out record.NumST, out record.AbsST, out record.OrdST, out record.PtsST)) return false;

      if (!FromGwaByFuncs(remainingItems, out remainingItems, (v) => AddNullableDoubleValue(v, out record.Eps))) return false;

      //Process uls / sls curves
      if (!AddMatCurveParam(remainingItems, out remainingItems, out record.Uls)) return false;
      if (!AddMatCurveParam(remainingItems, out remainingItems, out record.Sls)) return false;

      if (!FromGwaByFuncs(remainingItems, out remainingItems, (v) => AddNullableDoubleValue(v, out record.Cost), (v) => Enum.TryParse<MatType>(v, true, out record.Type))) return false;

      //TODO: process environmental variables
      if (remainingItems[0].ToUpper() == "NO") remainingItems = remainingItems.Skip(1).ToList();
      else
      {
        var concreteTypes = Enum.GetValues(typeof(MatConcreteType)).OfType<MatConcreteType>().Select(v => v.GetStringValue());
        var nextMatConcreteArg = remainingItems.FirstOrDefault(v => concreteTypes.Any(ct => v.Equals(ct, StringComparison.InvariantCultureIgnoreCase)));
        if (string.IsNullOrEmpty(nextMatConcreteArg))
        {
          return false;
        }
        remainingItems = remainingItems.Skip(remainingItems.IndexOf(nextMatConcreteArg)).ToList();
      }

      //Process final items
      return true;
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }
      if (record.Index == null) //embedded
      {
        items.RemoveAt(items.Count - 1);
      }
      //MAT.11 | num | name | E | f | nu | G | rho | alpha | <prop> | num_uc (| abs | ord | pts[] |) num_sc (| abs | ord | pts[] |) num_ut (| abs | ord | pts[] |) num_st (| abs | ord | pts[] |) eps | <uls> | <sls> | cost | type | env | env_param
      AddItems(ref items, record.Name, record.E, record.F, record.Nu, record.G, record.Rho, record.Alpha); //Add items up until <prop>
      AddObject(ref items, record.Prop); //Add GsaMatAnal object
      AddExplicitCurves(ref items); //Add items associated with the explicit curves
      AddItems(ref items, record.Eps);
      AddObject(ref items, record.Uls); //Add GsaMatCurveParam object
      AddObject(ref items, record.Sls); //Add GsaMatCurveParam object
      AddItems(ref items, record.Cost, record.Type, "NO"); //TODO: handle environmental variables

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private bool AddObject(ref List<string> items, GsaMatAnal x)
    {
      var gsaMatAnalParser = new GsaMatAnalParser(x);
      if (!gsaMatAnalParser.Gwa(out var gwa))
      {
        return false;
      }
      items.Add(gwa.First());
      return true;
    }

    private bool AddObject(ref List<string> items, GsaMatCurveParam x)
    {
      var parser = new GsaMatCurveParamParser(x);
      if (!parser.Gwa(out var gwa))
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
      if (record.NumUC > 0)
      {
        AddItems(ref items, record.NumUC, record.AbsUC, record.OrdUC, record.PtsUC);
      }
      else
      {
        AddItems(ref items, record.NumUC);
      }

      //SLS - Compression
      if (record.NumSC > 0)
      {
        AddItems(ref items, record.NumSC, record.AbsSC, record.OrdSC, record.PtsSC);
      }
      else
      {
        AddItems(ref items, record.NumSC);
      }

      //ULS - tension
      if (record.NumUT > 0)
      {
        AddItems(ref items, record.NumUT, record.AbsUT, record.OrdUT, record.PtsUT);
      }
      else
      {
        AddItems(ref items, record.NumUT);
      }

      //SLS - tension
      if (record.NumST > 0)
      {
        AddItems(ref items, record.NumST, record.AbsST, record.OrdST, record.PtsST);
      }
      else
      {
        AddItems(ref items, record.NumST);
      }
      return true;
    }
    #endregion

    #region from_gwa_fns

    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    private bool AddMatAnal(List<string> items, out List<string> remainingItems)
    {
      Join(items, out var matAnalGwa);
      var gsaMatAnalParser = new GsaMatAnalParser();
      if (!gsaMatAnalParser.FromGwa(matAnalGwa, out remainingItems))
      {
        remainingItems = items;
        return false;
      }
      record.Prop = (GsaMatAnal)gsaMatAnalParser.Record;
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
      var parser = new GsaMatCurveParamParser();
      Join(items, out var gwa);
      if (!parser.FromGwa(gwa, out remainingItems))
      {
        curve = (GsaMatCurveParam)parser.Record;
        remainingItems = items;
        return false;
      }
      curve = (GsaMatCurveParam)parser.Record;
      return true;
    }
    #endregion
  }
}
