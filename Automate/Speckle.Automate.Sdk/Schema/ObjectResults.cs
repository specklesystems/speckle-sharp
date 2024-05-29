namespace Speckle.Automate.Sdk.Schema;

public struct ObjectResults
{
  public readonly int Version => 1;
  public ObjectResultValues Values { get; set; }
}
