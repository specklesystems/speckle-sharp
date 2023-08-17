using System.Text.RegularExpressions;

namespace Speckle.Core.Helpers;

public static class Constants
{
  public const double Eps = 1e-5;
  public const double SmallEps = 1e-8;
  public const double Eps2 = Eps * Eps;
  
  public static readonly Regex ChunkPropertyNameRegex = new(@"^@\((\d*)\)"); //TODO: Experiment with compiled flag
}
