using System.Collections.Generic;

namespace Speckle.Core.Models;

/// <summary>
/// A simple container for organising objects within a model and preserving object hierarchy.
/// A container is defined by a human-readable <see cref="name"/>, a unique <see cref="applicationId"/>, and its list of contained <see cref="elements"/>.
/// The <see cref="elements"/> can include an unrestricted number of <see cref="Base"/> objects including additional nested <see cref="Collection"/>s.
/// </summary>
/// <remarks>
/// A <see cref="Collection"/> can be for example a Layer in Rhino/AutoCad, a collection in Blender, or a Category in Revit.
/// The location of each collection in the hierarchy of collections in a commit will be retrieved through commit traversal.
/// </remarks>
public class Collection : Base
{
  public Collection() { }

  /// <summary>
  /// Constructor for a basic collection.
  /// </summary>
  /// <param name="name">The human-readable name of this collection</param>
  public Collection(string name, string collectionType)
  {
    this.name = name;
    this.collectionType = collectionType;
  }

  /// <summary>
  /// The human-readable name of the <see cref="Collection"/>.
  /// </summary>
  /// <remarks>This name is not necessarily unique within a commit. Set the applicationId for a unique identifier.</remarks>
  public string name { get; set; }

  /// <summary>
  /// The type of this collection
  /// </summary>
  public string collectionType { get; set; }

  /// <summary>
  /// The elements contained in this <see cref="Collection"/>.
  /// </summary>
  /// <remarks>
  /// This can include additional nested <see cref="Collection"/>s.
  /// </remarks>
  [DetachProperty]
  public List<Base> elements { get; set; } = new();
}
