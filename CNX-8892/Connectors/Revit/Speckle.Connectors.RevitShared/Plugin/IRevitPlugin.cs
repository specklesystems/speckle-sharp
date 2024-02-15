using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Connectors.Revit.Plugin;

internal interface IRevitPlugin
{
  void Initialise();
  void Shutdown();
}
