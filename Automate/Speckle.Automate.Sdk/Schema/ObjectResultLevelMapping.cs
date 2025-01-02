namespace Speckle.Automate.Sdk.Schema;

public abstract class ObjectResultLevelMapping
{
  private const string SUCCESS = "SUCCESS";
  private const string INFO = "INFO";
  private const string WARNING = "WARNING";
  private const string ERROR = "ERROR";

  public static string Get(ObjectResultLevel level) =>
    level switch
    {
      ObjectResultLevel.Error => ERROR,
      ObjectResultLevel.Warning => WARNING,
      ObjectResultLevel.Info => INFO,
      ObjectResultLevel.Success => SUCCESS,
      _ => throw new ArgumentOutOfRangeException($"Not valid value for enum {level}")
    };
}
