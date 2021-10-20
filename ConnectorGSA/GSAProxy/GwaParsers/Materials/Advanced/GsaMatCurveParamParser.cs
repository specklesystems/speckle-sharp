using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [GsaType(GwaKeyword.MAT_CURVE_PARAM, GwaSetCommandType.SetAt, false)]
  public class GsaMatCurveParamParser : GwaParser<GsaMatCurveParam>
  {
    public GsaMatCurveParamParser(GsaMatCurveParam gsaMatCurveParam) : base(gsaMatCurveParam) { }

    public GsaMatCurveParamParser() : base(new GsaMatCurveParam()) { }

    public override bool FromGwa(string gwa)
    {
      return FromGwa(gwa, out var _);
    }

    public bool FromGwa(string gwa, out List<string> remainingItems)
    {
      if (!BasicFromGwa(gwa, out remainingItems))
      {
        return false;
      }

      //MAT_CURVE_PARAM.3 | num | name | model | strain[6] | gamma_f | gamma_e
      return FromGwaByFuncs(remainingItems, out remainingItems, AddName, AddModel, AddStrainElasticCompression,
        AddStrainElasticTension, AddStrainPlasticCompression, AddStrainPlasticTension, AddStrainFailureCompression,
        AddStrainFailureTension, AddGammaF, AddGammaE);
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //MAT_CURVE_PARAM.3 | num | name | model | strain[6] | gamma_f | gamma_e
      AddItems(ref items, record.Name, AddModel(), record.StrainElasticCompression, record.StrainElasticTension, 
        record.StrainPlasticCompression, record.StrainPlasticTension, record.StrainFailureCompression, 
        record.StrainFailureTension, record.GammaF, record.GammaE);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddModel()
    {
      var str = "";
      if (record.Model == null)
      {
        return "UNDEF";
      }
      for (var i = 0; i < record.Model.Count; i++)
      {
        str += record.Model[i].ToString() + "+";
      }
      return str.Remove(str.Length - 1);
    }
    #endregion

    #region from_gwa_fns
    protected bool AddName(string v)
    {
      record.Name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    private bool AddModel(string v)
    {
      var pieces = v.Split('+');
      record.Model = new List<MatCurveParamType>();
      foreach (var piece in pieces)
      {
        if (!Enum.TryParse<MatCurveParamType>(piece, true, out var value))
        {
          return false;
        }
        record.Model.Add(value);
      }
      return true;
    }

    private bool AddStrainElasticCompression(string v)
    {
      record.StrainElasticCompression = (double.TryParse(v, out var value) && value >= 0) ? (double?)value : null;
      return true;
    }

    private bool AddStrainElasticTension(string v)
    {
      record.StrainElasticTension = (double.TryParse(v, out var value) && value >= 0) ? (double?)value : null;
      return true;
    }

    private bool AddStrainPlasticCompression(string v)
    {
      record.StrainPlasticCompression = (double.TryParse(v, out var value) && value >= 0) ? (double?)value : null;
      return true;
    }

    private bool AddStrainPlasticTension(string v)
    {
      record.StrainPlasticTension = (double.TryParse(v, out var value) && value >= 0) ? (double?)value : null;
      return true;
    }

    private bool AddStrainFailureCompression(string v)
    {
      record.StrainFailureCompression = (double.TryParse(v, out var value) && value >= 0) ? (double?)value : null;
      return true;
    }

    private bool AddStrainFailureTension(string v)
    {
      record.StrainFailureTension = (double.TryParse(v, out var value) && value >= 0) ? (double?)value : null;
      return true;
    }

    private bool AddGammaF(string v)
    {
      record.GammaF = (double.TryParse(v, out var value)) ? (double?)value : null;
      return true;
    }

    private bool AddGammaE(string v)
    {
      record.GammaE = (double.TryParse(v, out var value)) ? (double?)value : null;
      return true;
    }
    #endregion
  }
}
