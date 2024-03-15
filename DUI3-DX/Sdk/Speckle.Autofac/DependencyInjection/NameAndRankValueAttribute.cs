using System;

namespace Speckle.Autofac.DependencyInjection;

[AttributeUsage(AttributeTargets.Class)]
public sealed class NameAndRankValueAttribute : Attribute
{
  public string Name { get; private set; }
  public int Rank { get; private set; }

  public NameAndRankValueAttribute(string name, int rank)
  {
    Name = name;
    Rank = rank;
  }
}
