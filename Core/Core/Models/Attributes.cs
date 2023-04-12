using System;

namespace Speckle.Core.Models;

/// <summary>
/// <para>Flags an object's property as being detachable.</para>
/// <para>If set to true the default serialiser will persist it separately, and add a reference to the property's value in the original object.</para>
/// <para>Only applies to properties of types derived from the Base class.</para>
/// </summary>
public class DetachProperty : Attribute
{
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

  public bool Detachable { get; set; } = true;
}

/// <summary>
/// Flags a list or array as splittable into chunks during serialisation. These chunks will be recomposed on deserialisation into the original list. Note: this attribute should be used in conjunction with <see cref="DetachProperty"/>.
/// <para>Use this attribute on properties that can become very long and are not worth detaching into individual elements.</para>
/// <para>Objects per chunk: for simple types, like numbers, use a high value (>10000); for other objects, use a more conservative number depending on their serialised size.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class Chunkable : Attribute
{
  public Chunkable(int ObjectsPerChunk = 1000)
  {
    MaxObjCountPerChunk = ObjectsPerChunk;
  }

  public int MaxObjCountPerChunk { get; set; }
}