using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Speckle.ConnectorGSA.Proxy.Results
{
  public enum ResultUnitType
  {
    None = 0,
    Force,
    Length,
    Disp,
    Mass,
    Time,
    Temp,
    Stress,
    Accel,
    Angle // not supported in GWA but added here to reflect its use in the UI; being unsupported in GWA, the code will hard-wire values
          //energy and others don't seem to be supported in GWA but also not needed in result extraction code so they're left out
  }

  //These span distance, force and other unit types, so that they can be put into an array which represents x per x per x, e.g. "N/m"
  internal enum ResultUnit
  {
    N,
    KN,
    mm,
    m,
    Pa,
    kPa,
    rad
  }
}
