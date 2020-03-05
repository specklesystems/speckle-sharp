using System;
using System.Collections.Generic;

namespace Speckle.Models
{

  public class Stream 
  {
    public string id { get; set; }

    public string name { get; set; }

    public string modelId { get; set; }

    public List<Revision> revisions { get; set; } = new List<Revision>();
  }

  public class Revision : Base
  {
    [DetachProperty(true)]
    public List<Base> objects { get; set; } = new List<Base>();

    public string name { get; set; }

    public string description { get; set; }

    public List<string> tags { get; set; } = new List<string>();

    public override string hash => base.hash;

    public Revision() : base() { }
  }

  /// <summary>
  /// Wrapper around other, thrid party, classes that are not speckle kits.
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

    public Abstract() { }

    public Abstract(object _original)
    {
      @base = _original;
      assemblyQualifiedName = @base.GetType().AssemblyQualifiedName;
    }

  }

  public class Reference
  {
    public string referencedId { get; set; }
    public string speckle_type = "reference";

    public Reference() { }
  }

}
