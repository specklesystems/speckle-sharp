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

  /// <summary>
  /// A group of speckle objects. Formerly known as a stream.
  /// </summary>
  public class Revision : Base
  {
    [DetachProperty(true)]
    public List<Base> objects { get; set; } = new List<Base>();

    public string name { get; set; }

    public string description { get; set; }

    public Revision() : base() { }
  }

  /// <summary>
  /// Base class for a given model (ie, file) that a stream can be hosted in.
  /// </summary>
  public class Model : Base
  {
    public string modelId { get; set; }

    public string name { get; set; }

    public string source { get; set; }

    public string location { get; set; }
  }

}
