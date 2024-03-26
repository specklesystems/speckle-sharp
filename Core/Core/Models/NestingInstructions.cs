namespace Speckle.Core.Models;

/// <summary>
/// Container for a reference to a parent's applicationId and an Action to
/// execute in order to nest the child on the parent
/// </summary>
public readonly struct NestingInstructions
{
  public delegate void NestAction(Base parent, Base child);

  public NestingInstructions(string? parentApplicationId, NestAction nestAction)
  {
    ParentApplicationId = parentApplicationId;
    Nest = nestAction;
  }

  public string? ParentApplicationId { get; }
  public NestAction Nest { get; }
}
