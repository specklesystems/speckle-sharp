namespace Speckle.ConnectorNavisworks.Entry;

public abstract class LaunchSpeckleConnector
{
  public const string Command = "Speckle_Launch";
  public const string Plugin = "SpeckleUI";
}

public abstract class RetryLastConversionSend
{
  public const string Command = "Speckle_RetryLastConversionSend";
  public const string Plugin = "SpeckleUI";
}

public abstract class Community
{
  public const string Command = "Speckle_Community";
}

public abstract class TurnPersistCacheOff
{
  public const string Command = "Speckle_PersistCache_Off";
}

public abstract class TurnPersistCacheOn
{
  public const string Command = "Speckle_PersistCache_On";
}
