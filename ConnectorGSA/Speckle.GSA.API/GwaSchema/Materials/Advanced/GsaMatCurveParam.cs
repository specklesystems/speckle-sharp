using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;


namespace Speckle.GSA.API.GwaSchema
{
  [GsaType(GwaKeyword.MAT_CURVE_PARAM, GwaSetCommandType.SetAt, false)]
  public class GsaMatCurveParam : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public List<MatCurveParamType> Model;
    public double? StrainElasticCompression;
    public double? StrainElasticTension;
    public double? StrainPlasticCompression;
    public double? StrainPlasticTension;
    public double? StrainFailureCompression;
    public double? StrainFailureTension;
    public double? GammaF;
    public double? GammaE;

    public GsaMatCurveParam() : base()
    {
      //Defaults
      Version = 3;
    }

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
      AddItems(ref items, Name, AddModel(), StrainElasticCompression, StrainElasticTension, StrainPlasticCompression, StrainPlasticTension, StrainFailureCompression, StrainFailureTension, GammaF, GammaE);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddModel()
    {
      var str = "";
      for (var i = 0; i < Model.Count; i++)
      {
        str += Model[i].ToString() + "+";
      }
      return str.Remove(str.Length - 1);
    }
    #endregion

    #region from_gwa_fns
    private bool AddModel(string v)
    {
      var pieces = v.Split('+');
      Model = new List<MatCurveParamType>();
      foreach (var piece in pieces)
      {
        if (!Enum.TryParse<MatCurveParamType>(piece, true, out var value))
        {
          return false;
        }
        Model.Add(value);
      }
      return true;
    }

    private bool AddStrainElasticCompression(string v)
    {
      StrainElasticCompression = (double.TryParse(v, out var value) && value >= 0) ? (double?)value : null;
      return true;
    }

    private bool AddStrainElasticTension(string v)
    {
      StrainElasticTension = (double.TryParse(v, out var value) && value >= 0) ? (double?)value : null;
      return true;
    }

    private bool AddStrainPlasticCompression(string v)
    {
      StrainPlasticCompression = (double.TryParse(v, out var value) && value >= 0) ? (double?)value : null;
      return true;
    }

    private bool AddStrainPlasticTension(string v)
    {
      StrainPlasticTension = (double.TryParse(v, out var value) && value >= 0) ? (double?)value : null;
      return true;
    }

    private bool AddStrainFailureCompression(string v)
    {
      StrainFailureCompression = (double.TryParse(v, out var value) && value >= 0) ? (double?)value : null;
      return true;
    }

    private bool AddStrainFailureTension(string v)
    {
      StrainFailureTension = (double.TryParse(v, out var value) && value >= 0) ? (double?)value : null;
      return true;
    }

    private bool AddGammaF(string v)
    {
      GammaF = (double.TryParse(v, out var value)) ? (double?)value : null;
      return true;
    }

    private bool AddGammaE(string v)
    {
      GammaE = (double.TryParse(v, out var value)) ? (double?)value : null;
      return true;
    }
    #endregion
  }
}
