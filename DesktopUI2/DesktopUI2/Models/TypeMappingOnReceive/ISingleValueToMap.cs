using System;
using System.Collections.Generic;
using System.Text;

namespace DesktopUI2.Models.TypeMappingOnReceive
{
  public interface ISingleValueToMap
  {
    public string IncomingType { get; set; }
    public string InitialGuess { get; set; }
    public string OutgoingType { get; set; }
    public string IncomingTypeDisplayName { get; }
  }
}
