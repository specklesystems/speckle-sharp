using Speckle.Core.Models;

namespace Objects.Structural.Loading;

public class Load : Base
{
  public Load() { }

  /// <summary>
  /// A generalised structural load, described by a name and load case
  /// </summary>
  /// <param name="name">Name of the load</param>
  /// <param name="loadCase">Load case specification for the load</param>
  public Load(string name, LoadCase loadCase)
  {
    this.name = name;
    this.loadCase = loadCase;
  }

  public string name { get; set; }

  [DetachProperty]
  public LoadCase loadCase { get; set; }

  public string units { get; set; }
}
