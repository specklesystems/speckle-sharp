using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DesktopUI2.Models;
using Speckle.Core.Logging;
using System;
using System.Collections.Generic;

namespace DesktopUI2.Views.Windows.Dialogs
{
  public partial class ImportExportAlert : Window
  {
    public Action LaunchAction { get; set; }
    public ImportExportAlert()
    {
      InitializeComponent();
    }

    private void InitializeComponent()
    {
      AvaloniaXamlLoader.Load(this);
    }

    public void Close_Click(object sender, RoutedEventArgs e)
    {
      this.Close();
    }

    public void OpenSpeckle_Click(object sender, RoutedEventArgs e)
    {
      Analytics.TrackEvent(Analytics.Events.ImportExportAlert, new Dictionary<string, object>() { { "name", "Open Speckle" } });
      LaunchAction.Invoke();
      this.Close();
    }

    public void DontShow_Click(object sender, RoutedEventArgs e)
    {
      Analytics.TrackEvent(Analytics.Events.ImportExportAlert, new Dictionary<string, object>() { { "name", "Disable" } });
      var config = ConfigManager.Load();
      config.ShowImportExportAlert = false;
      ConfigManager.Save(config);
      this.Close();
    }
  }
}
