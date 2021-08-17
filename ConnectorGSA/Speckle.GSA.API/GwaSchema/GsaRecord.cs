namespace Speckle.GSA.API.GwaSchema
{
  public abstract class GsaRecord
  {
    public int? Index;
    public int Version;
    public string Sid;
    public string ApplicationId;
    public string StreamId;

    //Not all objects have names, so it's up to the concrete classes to include a property which uses this field.
    //It's included here so the AddName method can be included in this abstract class, saving its repetition in concrete classes
    protected string name;


  }
}
