using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.Kits;
using Speckle.Models;

namespace Speckle.Core
{

  public class CoreKit : ISpeckleKit
  {
    public IEnumerable<Type> Types => GetType().Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(Base)));

    public IEnumerable<Type> Converters => GetType().Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(Converter)));

    public string Description => "Base Speckle models for revisions, streams, etc.";

    public string Name => nameof(CoreKit);

    public string Author => "Dimitrie";

    public string WebsiteOrEmail => "hello@speckle.works";

    public CoreKit() { }
  }

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

  public class ObjectReference
  {
    public string referencedId { get; set; }
    public string speckle_type = "reference";

    public ObjectReference() { }
  }

}
