namespace Speckle.Connectors.Revit.Plugin;

internal sealed class RevitSettings // POC: need to derive some interface for things that require IHostSettings
{
  public string? RevitPanelName { get; set; }
  public string? RevitVersionName { get; set; }
  public string? RevitTabName { get; set; }
  public string? RevitTabTitle { get; set; }
  public string? RevitButtonName { get; set; }
  public string? RevitButtonText { get; set; }
  public string? HostSlug { get; set; } // POC: from generic IHostSettings interface
  public string? HostAppVersion { get; set; } // POC as HostSlug??
  public string[]? ModuleFolders { get; set; }
}
