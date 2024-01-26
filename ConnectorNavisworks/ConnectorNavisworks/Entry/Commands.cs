namespace Speckle.ConnectorNavisworks.Entry;

public abstract class LaunchSpeckleConnector
{
  public const string COMMAND = "Speckle_Launch";
  public const string PLUGIN = "SpeckleUI";
}

public abstract class RetryLastConversionSend
{
  public const string COMMAND = "Speckle_RetryLastConversionSend";
  public const string PLUGIN = "SpeckleUI";
}

public abstract class Community
{
  public const string COMMAND = "Speckle_Community";
}

public abstract class TurnPersistCacheOff
{
  public const string COMMAND = "Speckle_PersistCache_Off";
}

public abstract class TurnPersistCacheOn
{
  public const string COMMAND = "Speckle_PersistCache_On";
}
