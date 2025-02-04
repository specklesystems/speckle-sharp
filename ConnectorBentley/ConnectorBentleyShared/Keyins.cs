using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Speckle.ConnectorBentley.Entry;

namespace Speckle.ConnectorBentley;

public class Keyins
{
  public static void Start(string unparsed)
  {
    SpeckleBentleyApp.Instance.Start(unparsed);
  }
}
