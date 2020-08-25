using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Core.Api
{
  public class UserStreamCreatedSubscriptionResult
  {
    string id { get; set; }
    string name { get; set; }
    string description { get; set; }
    bool isPublic { get; set; }
    string ownerId { get; set; }
  }
}
