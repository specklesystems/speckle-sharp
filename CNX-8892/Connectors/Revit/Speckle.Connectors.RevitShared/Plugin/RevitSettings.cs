using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SqlServer.Server;

namespace Speckle.Connectors.Revit.Plugin;

public class RevitSettings // POC: need to derive some interface for things that require IHostSettings
{
  public string RevitVersionName { get; set; }
  public string RevitTabName { get; set; }
  public string RevitTabTitle { get; set; }
  public string RevitButtonName { get; set; }
  public string RevitButtonText { get; set; }
  public string HostSlug { get; set; } // POC: from generic IHostSettings interface
  public string HostAppVersion { get; set; } // POC as HostSlug
}
