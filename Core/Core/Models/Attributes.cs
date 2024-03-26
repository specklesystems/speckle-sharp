using System;

namespace Speckle.Core.Models;

/// <summary>
/// <para>Flags an object's property as being detachable.</para>
/// <para>If set to true the default serialiser will persist it separately, and add a reference to the property's value in the original object.</para>
/// <para>Only applies to properties of types derived from the Base class.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DetachProperty : Attribute
{
  /// <summary>
  /// <para>Flags an object's property as being detachable.</para>
  /// <para>If set to true the default serialiser will persist it separately, and add a reference to the property's value in the original object.</para>
  /// <para>Only applies to properties of types derived from the Base class.</para>
  /// </summary>
  /// <param name="detachable">Whether to detach the property or not.</param>
  public DetachProperty(bool detachable = true)
  {
    Detachable = detachable;
  }

  public bool Detachable { get; }
}

/// <summary>
/// Flags a list or array as splittable into chunks during serialisation. These chunks will be recomposed on deserialisation into the original list. Note: this attribute should be used in conjunction with <see cref="DetachProperty"/>.
/// <para>Use this attribute on properties that can become very long and are not worth detaching into individual elements.</para>
/// <para>Objects per chunk: for simple types, like numbers, use a high value (>10000); for other objects, use a more conservative number depending on their serialised size.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class Chunkable : Attribute
{
  public Chunkable(int maxObjCountPerChunk = 1000)
  {
    MaxObjCountPerChunk = maxObjCountPerChunk;
  }

  public int MaxObjCountPerChunk { get; }
}
