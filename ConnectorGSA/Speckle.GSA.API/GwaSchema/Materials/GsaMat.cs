namespace Speckle.GSA.API.GwaSchema
{
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
      Version = 11;
    }
  }
}
