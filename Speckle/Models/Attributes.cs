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

  /// <summary>
  /// Flags wether to store inner references when serialising the decorated object type.
  /// </summary>
  public class ExtractInnerReferences : Attribute
  {
    public bool Save { get; set; } = true;

    /// <summary>
    /// <para>Tells the serialiser wether this class should have an inner references field added to it at the end of the serialisation.</para>
    /// <para>This filed will contain a list of all the hashes of all the subobjects.</para>
    /// </summary>
    /// <param name="_save">Wether to save or not the inner references array on this object.</param>
    public ExtractInnerReferences(bool _save = true)
    {
      Save = _save;
    }
  }
}
