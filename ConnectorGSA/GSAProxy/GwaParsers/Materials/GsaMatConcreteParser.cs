using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.MAT_CONCRETE, GwaSetCommandType.Set, true)]
  public class GsaMatConcreteParser : GwaParser<GsaMatConcrete>
  {
    public GsaMatConcreteParser(GsaMatConcrete gsaMatConcrete) : base(gsaMatConcrete) { }

    public GsaMatConcreteParser() : base(new GsaMatConcrete()) { }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      // Process <mat>
      if (!AddMat(remainingItems, out remainingItems)) return false;

      //MAT_CONCRETE | num | <mat> | type | cement | fc | fcd | fcdc | fcdt | fcfib | EmEs | Emod | n | ? | eps_peak | eps_max | eps_u | eps_ax | eps_tran | eps_axs | light | agg | xd_min | xd_max | beta | shrink | confine | fcc | eps_plas_c |eps_u_c
      return FromGwaByFuncs(remainingItems, out var _, (v) => Enum.TryParse<MatConcreteType >(v, true, out record.Type), (v) => Enum.TryParse<MatConcreteCement>(v, true, out record.Cement),
        (v) => AddNullableDoubleValue(v, out record.Fc), (v) => AddNullableDoubleValue(v, out record.Fcd), (v) => AddNullableDoubleValue(v, out record.Fcdc),
        (v) => AddNullableDoubleValue(v, out record.Fcdt), (v) => AddNullableDoubleValue(v, out record.Fcfib), (v) => AddNullableDoubleValue(v, out record.EmEs),
        (v) => AddNullableDoubleValue(v, out record.Emod), (v) => AddNullableDoubleValue(v, out record.N), (v) => AddNullableDoubleValue(v, out record.Eps), 
        (v) => AddNullableDoubleValue(v, out record.EpsPeak), (v) => AddNullableDoubleValue(v, out record.EpsMax), (v) => AddNullableDoubleValue(v, out record.EpsU), 
        (v) => AddNullableDoubleValue(v, out record.EpsAx), (v) => AddNullableDoubleValue(v, out record.EpsTran), (v) => AddNullableDoubleValue(v, out record.EpsAxs), AddLight,
        (v) => AddNullableDoubleValue(v, out record.Agg), (v) => AddNullableDoubleValue(v, out record.XdMin), (v) => AddNullableDoubleValue(v, out record.XdMax),
        (v) => AddNullableDoubleValue(v, out record.Beta), (v) => AddNullableDoubleValue(v, out record.Shrink), (v) => AddNullableDoubleValue(v, out record.Confine),
        (v) => AddNullableDoubleValue(v, out record.Fcc), (v) => AddNullableDoubleValue(v, out record.EpsPlasC), (v) => AddNullableDoubleValue(v, out record.EpsUC));
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //MAT_CONCRETE | num | <mat> | type | cement | fc | fcd | fcdc | fcdt | fcfib | EmEs | n | Emod | eps | eps_peak | eps_max | eps_u | eps_ax | eps_tran | eps_axs | light | agg | xd_min | xd_max | beta | shrink | confine | fcc | eps_plas_c |eps_u_c
      AddMat(ref items);
      AddItems(ref items, record.Type, record.Cement, record.Fc, record.Fcd, record.Fcdc, record.Fcdt, record.Fcfib, record.EmEs, record.Emod, record.N, record.Eps, record.EpsPeak, 
        record.EpsMax, record.EpsU, record.EpsAx, record.EpsTran, record.EpsAxs, record.Light ? "YES" : "NO", record.Agg, record.XdMin, record.XdMax, record.Beta, record.Shrink, record.Confine, 
        record.Fcc, record.EpsPlasC, record.EpsUC);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private bool AddMat(ref List<string> items)
    {
      var gsaMatParser = new GsaMatParser(record.Mat);
      if (!gsaMatParser.Gwa(out var gwa))
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
      Join(items, out var matGwa);
      var gsaMatParser = new GsaMatParser();
      if (!gsaMatParser.FromGwa(matGwa, out remainingItems))
      {
        remainingItems = items;
        return false;
      }
      record.Mat = (GsaMat)gsaMatParser.Record;
      return true;
    }

    private bool AddLight(string v)
    {
      if (v == "NO")
      {
        record.Light = false;
      }
      else
      {
        record.Light = true;
      }
      return true;
    }
    #endregion
  }
}
