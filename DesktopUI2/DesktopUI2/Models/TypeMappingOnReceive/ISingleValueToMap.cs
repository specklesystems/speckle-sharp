using System;
using System.Collections.Generic;
using System.Text;

namespace DesktopUI2.Models.TypeMappingOnReceive
{
  internal interface ISingleValueToMap
  {
    public string IncomingType { get; set; }
    public string InitialGuess { get; set; }
  }
}
