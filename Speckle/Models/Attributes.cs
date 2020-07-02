using System;

namespace Speckle.Models
{

  /// <summary>
  /// <para>Flags an object's property as being detachable.</para>
  /// <para>If set to true the default serialiser will persist it separately, and add a reference to the property's value in the original object.</para>
  /// <para>Only applies to properties of types derived from the Base class.</para>
  /// </summary>
  public class DetachProperty : Attribute
  {
    public bool Detachable { get; set; } = true;

    /// <summary>
    /// <para>Flags an object's property as being detachable.</para>
    /// <para>If set to true the default serialiser will persist it separately, and add a reference to the property's value in the original object.</para>
    /// <para>Only applies to properties of types derived from the Base class.</para>
    /// </summary>
    /// <param name="_detachable">Wether to detach the property or not.</param>
    public DetachProperty(bool _detachable)
    {
      Detachable = _detachable;
    }

    public DetachProperty()
    {
      Detachable = true;
    }
  }
}
