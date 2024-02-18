using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SqlServer.Server;

namespace Speckle.Connectors.Revit.Plugin;

public class RevitSettings
{
  public string RevitVersionName { get; set; }
  public string RevitTabName { get; set; }
  public string RevitTabTitle { get; set; }
  public string RevitButtonName { get; set; }
  public string RevitButtonText { get; set; }
}
