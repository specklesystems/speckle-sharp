using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Speckle.ConnectorMicroStationOpenRoads.Entry;

namespace Speckle.ConnectorMicroStationOpenRoads
{
  public class Keyins
  {
    public static void Start(string unparsed)
    {
      SpeckleMicroStationOpenRoadsApp.Instance.Start(unparsed);
    }
  }
}
