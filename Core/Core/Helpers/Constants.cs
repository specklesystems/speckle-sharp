using System;
using System.Text.RegularExpressions;

namespace Speckle.Core.Helpers;

public static class Constants
{
  public const double EPS = 1e-5;
  public const double SMALL_EPS = 1e-8;
  public const double EPS_SQUARED = EPS * EPS;

  public static readonly Regex ChunkPropertyNameRegex = new(@"^@\((\d*)\)"); //TODO: Experiment with compiled flag

  [Obsolete("Renamed to " + nameof(EPS))]
  public const double Eps = 1e-5;

  [Obsolete("Renamed to " + nameof(SMALL_EPS))]
  public const double SmallEps = 1e-8;

  [Obsolete("Renamed to " + nameof(EPS_SQUARED))]
  public const double Eps2 = Eps * Eps;
}
