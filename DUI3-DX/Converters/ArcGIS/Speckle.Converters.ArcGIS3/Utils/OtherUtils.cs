using System.Text.RegularExpressions;

namespace Speckle.Converters.ArcGIS3.Utils;

public class OtherUtils : IOtherUtils
{
  public string CleanCharacters(string key)
  {
    Regex rg = new(@"^[a-zA-Z0-9_]*$");
    if (rg.IsMatch(key))
    {
      return key;
    }

    string result = "";
    foreach (char c in key)
    {
      Regex rg_character = new(@"^[a-zA-Z0-9_]*$");
      if (rg_character.IsMatch(c.ToString()))
      {
        result += c.ToString();
      }
      else
      {
        result += "_";
      }
    }
    return key.Replace(" ", "_").Replace("%", "_").Replace("$", "_");
  }
}
