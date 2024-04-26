using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Connectors.RevitShared.RevitPlugin;

internal interface IRevitPlugin
{
  void Initialise();
  void Shutdown();
}
