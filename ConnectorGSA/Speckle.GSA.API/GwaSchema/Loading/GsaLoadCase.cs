namespace Speckle.GSA.API.GwaSchema
{
  public class GsaLoadCase : GsaRecord
  {
    public StructuralLoadCaseType CaseType;
    public string Title;
    public int? Source;
    public LoadCategory Category;
    public AxisDirection3 Direction;
    public IncludeOption Include;
    public bool? Bridge;

    public GsaLoadCase() : base()
    {
      //Defaults
      Version = 2;
    }

  }
}
