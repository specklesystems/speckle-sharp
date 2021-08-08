using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;


namespace Speckle.GSA.API.GwaSchema
{
  [GsaType(GwaKeyword.MAT_CONCRETE, GwaSetCommandType.Set, true)]
  public class GsaMatConcrete : GsaRecord
  {
    //Based on the example below, the current documentation doesn't align with the GSA 10.1 keyword example. 
    //A bug ticket has been placed with the GSA developers. This class will need to be updated once the documentation is up to date
    //
    //   MAT_CONCRETE, num, <mat>,     type, cement,       fc,      fcd,      fcdc,        fcdt,       fcfib, EmEs, n, Emod, eps_peak, eps_max,   eps_u, eps_ax, eps_tran, eps_axs,  light, agg,   0.0, xd_min, xd_max,  beta, shrink, confine, fcc, eps_plas_c, eps_u_c
    //MAT_CONCRETE.17,   1, <mat>, CYLINDER,      N, 40000000, 34000000,  16000000, 3794733.192, 2276839.915,    0, 1,    2,    0.003,   0.003, 0.00069,  0.003,   0.0025,   0.002, 0.0025,  NO,  0.02,      0,       1, 0.77,      0,       0,   0,          0,       0

    public string Name { get => name; set { name = value; } }
    public GsaMat Mat;
    public MatConcreteType Type;
    public MatConcreteCement Cement;
    public double? Fc;
    public double? Fcd;
    public double? Fcdc;
    public double? Fcdt;
    public double? Fcfib;
    public double? EmEs;
    public double? N;
    public double? Emod;
    public double? EpsPeak;
    public double? EpsMax;
    public double? EpsU;
    public double? EpsAx;
    public double? EpsTran;
    public double? EpsAxs;
    public bool Light;
    public double? Agg;
    public double? XdMin;
    public double? XdMax;
    public double? Beta;
    public double? Shrink;
    public double? Confine;
    public double? Fcc;
    public double? EpsPlasC;
    public double? EpsUC;

    public GsaMatConcrete() : base()
    {
      //Defaults
      Version = 17;
    }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      // Process <mat>
      if (!AddMat(remainingItems, out remainingItems)) return false;

      //MAT_CONCRETE | num | <mat> | type | cement | fc | fcd | fcdc | fcdt | fcfib | EmEs | n | Emod | eps_peak | eps_max | eps_u | eps_ax | eps_tran | eps_axs | light | agg | 0.0 | xd_min | xd_max | beta | shrink | confine | fcc | eps_plas_c |eps_u_c
      return FromGwaByFuncs(remainingItems, out var _, (v) => Enum.TryParse<MatConcreteType>(v, true, out Type), (v) => Enum.TryParse<MatConcreteCement>(v, true, out Cement), 
        (v) => AddNullableDoubleValue(v, out Fc), (v) => AddNullableDoubleValue(v, out Fcd), (v) => AddNullableDoubleValue(v, out Fcdc),
        (v) => AddNullableDoubleValue(v, out Fcdt), (v) => AddNullableDoubleValue(v, out Fcfib), (v) => AddNullableDoubleValue(v, out EmEs),
        (v) => AddNullableDoubleValue(v, out N), (v) => AddNullableDoubleValue(v, out Emod), (v) => AddNullableDoubleValue(v, out EpsPeak),
        (v) => AddNullableDoubleValue(v, out EpsMax), (v) => AddNullableDoubleValue(v, out EpsU), (v) => AddNullableDoubleValue(v, out EpsAx),
        (v) => AddNullableDoubleValue(v, out EpsTran), (v) => AddNullableDoubleValue(v, out EpsAxs), AddLight,
        (v) => AddNullableDoubleValue(v, out Agg), null, (v) => AddNullableDoubleValue(v, out XdMin), (v) => AddNullableDoubleValue(v, out XdMax),
        (v) => AddNullableDoubleValue(v, out Beta), (v) => AddNullableDoubleValue(v, out Shrink), (v) => AddNullableDoubleValue(v, out Confine),
        (v) => AddNullableDoubleValue(v, out Fcc), (v) => AddNullableDoubleValue(v, out EpsPlasC), (v) => AddNullableDoubleValue(v, out EpsUC));
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //MAT_CONCRETE | num | <mat> | type | cement | fc | fcd | fcdc | fcdt | fcfib | EmEs | n | Emod | eps_peak | eps_max | eps_u | eps_ax | eps_tran | eps_axs | light | agg | 0.0 | xd_min | xd_max | beta | shrink | confine | fcc | eps_plas_c |eps_u_c
      AddMat(ref items);
      AddItems(ref items, Type, Cement, Fc, Fcd, Fcdc, Fcdt, Fcfib, EmEs, N, Emod, EpsPeak, EpsMax, EpsU, EpsAx, EpsTran, EpsAxs, Light, Agg, 0.0, XdMin, XdMax,
        Beta, Shrink, Confine, Fcc, EpsPlasC, EpsUC);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private bool AddMat(ref List<string> items)
    {
      if (!Mat.Gwa(out var gwa))
      {
        return false;
      }
      items.Add(gwa.First());
      return true;
    }
    #endregion

    #region from_gwa_fns
    private bool AddMat(List<string> items, out List<string> remainingItems)
    {
      Mat = new GsaMat();
      Join(items, out var matGwa);
      if (!Mat.FromGwa(matGwa, out remainingItems))
      {
        remainingItems = items;
        return false;
      }
      return true;
    }

    private bool AddLight(string v)
    {
      if (v == "NO")
      {
        Light = false;
      }
      else
      {
        Light = true;
      }
      return true;
    }
    #endregion
  }
}

