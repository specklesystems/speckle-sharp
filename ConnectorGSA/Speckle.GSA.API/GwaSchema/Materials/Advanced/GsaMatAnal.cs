namespace Speckle.GSA.API.GwaSchema
{
  public class GsaMatAnal : GsaRecord
  {
    //The current documentation doesn't align with the GSA 10.1 keyword example. 
    //A bug ticket has been placed with the GSA developers. This class will need to be updated once the documentation is up to date.
    //
    //In most cases, 2 undocuemnted parameters are included in the current GSA 10.1 keywords. These are ignored as they are zero. 
    //When creating a gwa string, 2 additional zeros are added at the end corresponding to the 2 undocuemented parameters.

    public MatAnalType Type;
    public string Name { get => name; set { name = value; } }  //If not embedded
    public Colour Colour = Colour.NO_RGB; //If not embedded
    public int? NumParams;
    #region MAT_ELAS_ISO
    public double? E;
    public double? Nu;
    public double? Rho;
    public double? Alpha;
    public double? G;
    public double? Damp;
    #endregion
    #region MAT_ELAS_PLAS_ISO
    public double? Yield;
    public double? Ultimate;
    public double? Eh;
    public double? Beta;
    #endregion
    #region MAT_DRUCKER_PRAGER
    public double? Cohesion;
    public double? Phi;
    public double? Psi;
    public double? Scribe;
    #endregion
    #region MAT_ELAS_ORTHO
    public double? Ex;
    public double? Ey;
    public double? Ez;
    public double? Nuxy;
    public double? Nuyz;
    public double? Nuzx;
    public double? Alphax;
    public double? Alphay;
    public double? Alphaz;
    public double? Gxy;
    public double? Gyz;
    public double? Gzx;
    #endregion
    #region MAT_FABRIC
    public double? Comp;
    #endregion
    #region MAT_MOHR_COULOMB
    #endregion

    public GsaMatAnal() : base()
    {
      //Defaults
      Version = 1;
    }
  }
}
