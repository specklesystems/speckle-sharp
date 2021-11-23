using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Speckle.ConnectorMicroStationOpen.Entry;

namespace Speckle.ConnectorMicroStationOpen
{
  public class Keyins
  {
    public static void Start(string unparsed)
    {
      SpeckleMicroStationOpenApp.Instance.Start(unparsed);
    }
  }
}
