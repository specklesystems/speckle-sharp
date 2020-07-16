
using System.Collections.Generic;

namespace Speckle.Core.GqlModels
{
  public class StreamInput
  {
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public bool isPublic { get; set; }
  }

  public class Stream
  {
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }

    public bool isPublic { get; set; }

    public string createdAt { get; set; }
    public string updatedAt { get; set; }

    public string role { get; set; }
  }

  public class User
  {
    public string id { get; set; }
    public string name { get; set; }
    public string role { get; set; }
    public List<Stream> streams { get; set; }
  }
}