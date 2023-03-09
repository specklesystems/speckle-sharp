using System;
using Speckle.Core.Logging;

namespace Speckle.Core.Models
{
  public class InvalidPropNameException : SpeckleException
  {

    public InvalidPropNameException(string propName, string reason) : base($"Property '{propName}' is invalid: {reason}")
    {
    }
  }
}
