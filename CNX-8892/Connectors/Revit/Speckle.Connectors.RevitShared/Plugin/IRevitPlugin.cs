using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.UI;

namespace Speckle.Connectors.Revit.Plugin;

internal interface IRevitPlugin
{
  void Initialise();
  void Shutdown();
}
