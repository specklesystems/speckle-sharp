using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DesktopUI2.Models;
using Speckle.Core.Logging;

namespace DesktopUI2.Views.Windows.Dialogs;

public class ImportExportAlert : Window
{
  public ImportExportAlert()
  {
    InitializeComponent();
  }

  public Action LaunchAction { get; set; }

  private void InitializeComponent()
  {
    AvaloniaXamlLoader.Load(this);
  }

  public void Close_Click(object sender, RoutedEventArgs e)
  {
    Close();
  }

  public void OpenSpeckle_Click(object sender, RoutedEventArgs e)
  {
    Analytics.TrackEvent(
      Analytics.Events.ImportExportAlert,
      new Dictionary<string, object> { { "name", "Open Speckle" } }
    );
    LaunchAction.Invoke();
    Close();
  }

  public void DontShow_Click(object sender, RoutedEventArgs e)
  {
    Analytics.TrackEvent(Analytics.Events.ImportExportAlert, new Dictionary<string, object> { { "name", "Disable" } });
    var config = ConfigManager.Load();
    config.ShowImportExportAlert = false;
    ConfigManager.Save(config);
    Close();
  }
}
