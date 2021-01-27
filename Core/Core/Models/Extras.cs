using Speckle.Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Speckle.Core.Models
{
  /// <summary>
  /// Wrapper around other, thrid party, classes that are not coming from a speckle kit.
  /// <para>Serialization and deserialization of the base object happens through default Newtonsoft converters. If your object does not de/serialize correctly, this class will not prevent that from happening.</para>
  /// <para><b>Limitations:</b></para>
  /// <para>- Base object needs to be serializable.</para>
  /// <para>- Inline collection declarations with values do not behave correctly.</para>
  /// <para>- Your class needs to have a void constructor.</para>
  /// <para>- Probably more. File a bug!</para>
  /// </summary>
  public class Abstract : Base
  {
    public string assemblyQualifiedName { get; set; }

    private object _base;

    /// <summary>
    /// The original object.
    /// </summary>
    public object @base
    {
      get => _base; set
      {
        _base = value;
        assemblyQualifiedName = value.GetType().AssemblyQualifiedName;
      }
    }

    /// <summary>
    /// See <see cref="Abstract"/> for limitations of this approach.
    /// </summary>
    public Abstract() { }

    /// <summary>
    /// See <see cref="Abstract"/> for limitations of this approach.
    /// </summary>
    /// <param name="_original"></param>
    public Abstract(object _original)
    {
      @base = _original;
      assemblyQualifiedName = @base.GetType().AssemblyQualifiedName;
    }

  }

  /// <summary>
  /// <para>In short, this helps you chunk big things into smaller things.</para>
  /// See the following <see href="https://pics.me.me/chunky-boi-57848570.png">reference.</see>
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class DataChunk : Base
  {
    public List<object> data { get; set; } = new List<object>();
    public DataChunk() { }
  }

  public class ObjectReference
  {
    public string referencedId { get; set; }
    public string speckle_type = "reference";

    public ObjectReference() { }
  }

  public class ProgressEventArgs : EventArgs
  {
    public int current { get; set; }
    public int total { get; set; }
    public string scope { get; set; }
    public ProgressEventArgs(int current, int total, string scope)
    {
      this.current = current; this.total = total; this.scope = scope;
    }
  }

  /// <summary>
  /// A simple wrapper to keep track of the relationship between speckle objects and their host-application siblings in cases where the
  /// <see cref="Base.applicationId"/> cannot correspond with the <see cref="ApplicationPlaceholderObject.ApplicationGeneratedId"/> (ie, on receiving operations). 
  /// </summary>
  public class ApplicationPlaceholderObject : Base
  {
    public ApplicationPlaceholderObject() { }

    public string ApplicationGeneratedId { get; set; }

    [JsonIgnore]
    public object NativeObject;
  }
}
