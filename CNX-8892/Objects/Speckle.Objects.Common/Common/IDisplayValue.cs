namespace Speckle.Objects.Common;

// POC: still accurate?
/// <summary>
/// Specifies displayable <see cref="Base"/> value(s) to be used as a fallback
/// if a displayable form cannot be converted.
/// </summary>
/// <example>
/// <see cref="Base"/> objects that represent conceptual / abstract / mathematically derived geometry
/// can use <see cref="displayValue"/> to be used in case the object lacks a natively displayable form.
/// (e.g <see cref="Spiral"/>, <see cref="Wall"/>, <see cref="Pipe"/>)
/// </example>
/// <typeparam name="T">
/// Type of display value.
/// Expected to be either a <see cref="Base"/> type or a <see cref="List{T}"/> of <see cref="Base"/>s,
/// most likely <see cref="Mesh"/> or <see cref="Polyline"/>.
/// </typeparam>
public interface IDisplayValue<T>
{
  /// <summary>
  /// <see cref="displayValue"/> <see cref="Base"/>(s) will be used to display this <see cref="Base"/>
  /// if a native displayable object cannot be converted.
  /// </summary>
  // POC: Pascal case? Is this for JS benefit?
  T displayValue { get; set; }
}
