namespace Speckle.Connectors.Revit.Plugin;

internal record RevitSettings( // POC: need to derive some interface for things that require IHostSettings{
  string RevitPanelName,
  string RevitTabName,
  string RevitTabTitle,
  string RevitVersionName,
  string RevitButtonName,
  string RevitButtonText,
  string[] ModuleFolders,
  string HostSlug,
  string HostAppVersion
);
