using System;
using System.Diagnostics.CodeAnalysis;

namespace Speckle.Core.Models.GraphTraversal;

[SuppressMessage(
  "Naming",
  "CA1708:Identifiers should differ by more than case",
  Justification = "Class contains obsolete members that are kept for backwards compatiblity"
)]
public class TraversalContext
{
  public Base Current { get; }
  public TraversalContext? Parent { get; }
  public string? PropName { get; }

  public TraversalContext(Base current, string? propName = null, TraversalContext? parent = default)
    : this(current, propName)
  {
    Parent = parent;
  }

  protected TraversalContext(Base current, string? propName = null)
  {
    Current = current;
    PropName = propName;
  }

  #region Obsolete

  [Obsolete("Renamed to " + nameof(Current))]
  [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Obsolete")]
  public Base current => Current;

  [Obsolete("Renamed to " + nameof(PropName))]
  [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Obsolete")]
  public string? propName => PropName;

  [Obsolete("Renamed to " + nameof(Parent))]
  [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Obsolete")]
  public TraversalContext? parent => Parent;

  #endregion
}

public class TraversalContext<T> : TraversalContext
  where T : TraversalContext
{
  public new T? Parent => (T?)base.Parent;

  public TraversalContext(Base current, string? propName = null, T? parent = default)
    : base(current, propName, parent) { }

  [Obsolete("Use " + nameof(Parent) + " instead")]
  [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Obsolete")]
  public T? typedParent => Parent;
}
