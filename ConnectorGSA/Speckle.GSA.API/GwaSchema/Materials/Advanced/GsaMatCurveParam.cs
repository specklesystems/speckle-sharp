using System.Collections.Generic;


namespace Speckle.GSA.API.GwaSchema
{
  public class GsaMatCurveParam : GsaRecord_
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

  }
}
