using System;
using Speckle.Core.Models;

namespace Objects.Organization
{
  public class ApplicationIdReference : Base
  {
    [Obsolete("This constructor is only for serialization purposes", true)]
    public ApplicationIdReference() { }

    public ApplicationIdReference(string applicationId)
    {
      this.applicationId = applicationId;
    }
  }
}
