namespace Speckle.GSA.API.GwaSchema
{
  public class GsaCombination : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public string Desc;
    public bool? Bridge;
    public string Note;

    public GsaCombination()
    {
      Version = 1;
    }
  }
}
