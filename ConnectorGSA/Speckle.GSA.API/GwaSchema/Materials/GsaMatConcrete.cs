namespace Speckle.GSA.API.GwaSchema
{
  public class GsaMatConcrete : GsaRecord
  {
    //Based on the example below, the current documentation doesn't align with the GSA 10.1 keyword example. 
    //A bug ticket has been placed with the GSA developers. This class will need to be updated once the documentation is up to date
    //
    //   MAT_CONCRETE, num, <mat>,     type, cement,       fc,      fcd,      fcdc,        fcdt,       fcfib, EmEs, Emod, n,   eps, eps_peak, eps_max, eps_u, eps_ax, eps_tran, eps_axs,  light,  agg, xd_min, xd_max,  beta, shrink, confine, fcc, eps_plas_c, eps_u_c
    //MAT_CONCRETE.17,   1, <mat>, CYLINDER,      N, 40000000, 34000000,  16000000, 3794733.192, 2276839.915,    0, 1,    2, 0.003,    0.003, 0.00069, 0.003, 0.0025,    0.002,  0.0025,     NO, 0.02,      0,       1, 0.77,      0,       0,   0,          0,       0

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
    public double? Emod;
    public double? N;
    public double? Eps; //undocumented
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

  }
}

