using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements;

public abstract class Baseline : Base
{
  protected Baseline() { }

  protected Baseline(string name, bool isFeaturelineBased)
  {
    this.name = name;
    this.isFeaturelineBased = isFeaturelineBased;
  }

  /// <summary>
  /// The name of this baseline
  /// </summary>
  public string name { get; set; }

  /// <summary>
  /// The horizontal component of this baseline
  /// </summary>
  public abstract Alignment? alignment { get; internal set; }

  /// <summary>
  /// The vertical component of this baseline
  /// </summary>
  public abstract Profile? profile { get; internal set; }

  [DetachProperty]
  public Featureline? featureline { get; internal set; }

  public bool isFeaturelineBased { get; set; }

  public string units { get; set; }
}

/// <summary>
/// Generic instance class
/// </summary>
public abstract class Baseline<TA, TP> : Baseline
  where TA : Alignment
  where TP : Profile
{
  protected Baseline(string name, TA alignment, TP profile, Featureline? featureline, bool isFeaturelineBased)
    : base(name, isFeaturelineBased)
  {
    this.name = name;
    typedAlignment = alignment;
    typedProfile = profile;
    this.featureline = featureline;
    this.isFeaturelineBased = isFeaturelineBased;
  }

  protected Baseline()
    : base(string.Empty, false) { }

  [JsonIgnore]
  public TA typedAlignment { get; set; }

  [JsonIgnore]
  public TP typedProfile { get; set; }

  [DetachProperty]
  public override Alignment? alignment
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
  public override Profile? profile
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
