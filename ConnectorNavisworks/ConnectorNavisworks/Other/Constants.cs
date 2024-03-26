namespace Speckle.ConnectorNavisworks.Other;

public static class Constants
{
  public const string ROOT_NODE_PSEUDO_ID = "___";

  public enum ConversionState
  {
    Converted = 0,
    Skipped = 1,
    ToConvert = 2,
    Failed = 3
  }
}
