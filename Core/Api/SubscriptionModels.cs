using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Core.Api.SubscriptionModels
{
  public class UserStreamCreatedResult
  {
    public UserStreamCreatedContent UserStreamCreated { get; set; }
  }

  public class UserStreamCreatedContent
  {
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
  }

  public class StreamUpdatedResult
  {
    public StreamUpdatedContent StreamUpdated { get; set; }
  }

  public class StreamUpdatedContent
  {
    public string name { get; set; }
    public string description { get; set; }
  }
}
