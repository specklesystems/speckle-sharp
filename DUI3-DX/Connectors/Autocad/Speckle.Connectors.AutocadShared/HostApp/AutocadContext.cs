namespace Speckle.Connectors.Autocad.HostApp;

public class AutocadContext
{
  // POC: we may want to inject this autocadcontext with the active doc?

  private const string INVALID_CHARS = @"<>/\:;""?*|=,‘";

  // POC: we can move this function into more relevant place.
  public string RemoveInvalidChars(string str)
  {
    foreach (char c in INVALID_CHARS)
    {
      str = str.Replace(c.ToString(), string.Empty);
    }

    return str;
  }
}
