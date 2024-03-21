using System;

namespace Speckle.Converters.Common;

// POC: maybe better to put in utils/reflection
[AttributeUsage(AttributeTargets.Class)]
public sealed class NameAndRankValueAttribute : Attribute
{
  // DO NOT CHANGE! This is the base, lowest rank for a conversion
  public const int SPECKLE_DEFAULT_RANK = 0;

  public string Name { get; private set; }
  public int Rank { get; private set; }

  public NameAndRankValueAttribute(string name, int rank)
  {
    Name = name;
    Rank = rank;
  }
}
