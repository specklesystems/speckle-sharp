namespace Speckle.Automate.Sdk.Schema;

public struct ObjectResults
{
  public readonly string Version => "1.0.0";
  public ObjectResultValues Values { get; set; }
}
