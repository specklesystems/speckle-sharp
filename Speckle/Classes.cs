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

    public Revision() : base() { }

    public override string hash => base.hash; 
  }

  public class Reference
  {
    public string referencedId { get; set; }

    public Reference() { }
  }

}
