namespace Speckle.Automate.Sdk.Schema;

public abstract class ObjectResultLevelMapping
{
  private const string Info = "INFO";
  private const string Warning = "WARNING";
  private const string Error = "ERROR";

  public static string Get(ObjectResultLevel level)
  {
    return level switch
    {
      ObjectResultLevel.Error => Error,
      ObjectResultLevel.Warning => Warning,
      ObjectResultLevel.Info => Info,
      _ => throw new ArgumentOutOfRangeException($"Not valid value for enum {level}")
    };
  }
}
