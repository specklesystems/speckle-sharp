using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements;

public abstract class Baseline : Base
{
  public Baseline() { }

  public Baseline(string name)
  {
    this.name = name;
  }

  /// <summary>
  /// The name of this baseline
  /// </summary>
  public string name { get; set; }

  /// <summary>
  /// The horizontal component of this baseline
  /// </summary>
  public abstract Alignment alignment { get; internal set; }

  /// <summary>
  /// The vertical component of this baseline
  /// </summary>
  public abstract Profile profile { get; internal set; }
}

/// <summary>
/// Generic instance class
/// </summary>
public abstract class Baseline<TA, TP> : Baseline
  where TA : Alignment
  where TP : Profile
{
  protected Baseline(string name, TA alignment, TP profile)
    : base(name)
  {
    this.name = name;
    typedAlignment = alignment;
    typedProfile = profile;
  }

  protected Baseline()
    : base(string.Empty) { }

  [JsonIgnore]
  public TA typedAlignment { get; set; }

  [JsonIgnore]
  public TP typedProfile { get; set; }

  [DetachProperty]
  public override Alignment alignment
  {
    get => typedAlignment;
    internal set
    {
      if (value is TA typeA)
      {
        typedAlignment = typeA;
      }
    }
  }

  [DetachProperty]
  public override Profile profile
  {
    get => typedProfile;
    internal set
    {
      if (value is TP typeP)
      {
        typedProfile = typeP;
      }
    }
  }
}
