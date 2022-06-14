using System.Collections.Generic;
using Speckle.Core.Models;

namespace Objects.Organization
{
  /// <summary>
  /// A simple container for organising objects within a model and preserving object hierarchy.
  /// A container is defined by a unique <see cref="name"/> and its list of contained <see cref="elements"/>.
  /// The <see cref="elements"/> can include an unrestricted number of <see cref="Base"/> objects including additional nested <see cref="Container"/>s.
  /// </summary>
  /// <remarks>
  /// A <see cref="Container"/> correlates to eg Rhino and AutoCad Layers or Blender Collections.
  /// While it is possible for an object to belong to multiple containers, most applications will not support this and
  /// on receive the object will be assigned the the first container it is encountered in.
  /// </remarks>
  public class Container : Base
  {
    /// <summary>
    /// The name of the container.
    /// This should be unique within the commit.
    /// </summary>
    /// TODO: standardise this behaviour across connectors
    /// <remarks>On receive, this will be prepended with the id of the stream as to not overwrite existing containers in the file.</remarks>
    public string name { get; set; }

    /// <summary>
    /// The elements contained in this <see cref="Container"/>. This can include any <see cref="Base"/> object including
    /// additional nested <see cref="Container"/>s.
    /// </summary>
    /// <remarks>
    /// Most applications will expect all contained elements to have displayable geometry or to be another <see cref="Container"/>.
    /// This means that purely data <see cref="Base"/> objects may be lost on receive. 
    /// </remarks>
    [DetachProperty]
    public List<Base> elements { get; set; }
    
    public Container() {}

    /// <summary>
    /// Constructor for a basic container.
    /// </summary>
    /// <param name="name">The unique name of this container</param>
    /// <param name="elements">Any contained <see cref="Base"/> objects</param>
    public Container(string name, List<Base> elements = null)
    {
      this.name = name;
      this.elements = elements;
    }
  }
}