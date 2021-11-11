using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Collections.Generic;
using System.Linq;

namespace SpeckleUpdater
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {

    private bool _showProgress = false;
    private string _path = "";
    private string folder = Path.Combine(Path.GetTempPath(), Globals.AppName);
    private List<string> _processes = new List<string> { "rhino", "revit", "dynamo", "gsa", "acad", "ETABS" };

    public MainWindow(bool showProgress)
    {
      _showProgress = showProgress;
      InitializeComponent();
      try
      {
        if (!_showProgress)
        {
          Hide();
        }

        CheckForUpdates();
      }
      catch (Exception ex)
      {
        File.WriteAllText(Path.Combine(folder, "error_log.txt"), ex.ToString());
        System.Threading.Thread.Sleep(2000);
        Close();
      }
    }


    private async void CheckForUpdates()
    {
      var release = await Api.GetLatestRelease();

      if (release == null)
      {
        UpdateMessage.Text = $"You already have the latest {Globals.AppName}! {release.Name}";
        OkBtn.Visibility = Visibility.Visible;
        if (!_showProgress)
        {
          Close();
        }
      }

      else if (!UpdateAvailable(release))
      {
        UpdateMessage.Text = $"You already have the latest {Globals.AppName}! {release.Name}";
        OkBtn.Visibility = Visibility.Visible;
        if (!_showProgress)
        {
          Close();
        }
      }

      //get latest version
      else
      {
        _path = Path.Combine(folder, release.FileName);
        
        //don't download if already there
        if (!File.Exists(_path) || !AlreadyDownloaded(release.Name))
        {
          await Api.DownloadRelease(release.Url, folder, _path);
        }

        //double check!
        if (!File.Exists(_path))
        {
          UpdateMessage.Text = $"Something went wrong, try again later... ಥ_ಥ";
          OkBtn.Visibility = Visibility.Visible;
        }

        if (_processes.Any(x => IsProcessRunning(x)) || _showProgress)
        {
          Show();
          YesBtn.Visibility = Visibility.Visible;
          NoBtn.Visibility = Visibility.Visible;
          UpdateMessage.Text = $"{Globals.AppName} {release.Name} is available! Do you want to install it now?";
        }
        else
        {
          //silent install
          Process.Start(_path, "/SP- /VERYSILENT /SUPPRESSMSGBOXES");
          Close();
        }
      }
    }

    private bool UpdateAvailable(Release release)
    {
      var latest = Version.Parse(release.Name.Replace("v", ""));
      var current = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

      if (current.CompareTo(latest) >= 0)
      {
        return false;
      }

      return true;
    }

    private bool IsProcessRunning(string name)
    {
      var processes = Process.GetProcesses();
      foreach (var p in processes)
      {
        if (p.ProcessName.ToLowerInvariant().Contains(name))
        {
          return true;
        }
      }
      return false;
    }

    private bool AlreadyDownloaded(string version)
    {
      FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(_path);
      try
      {
        var v = Version.Parse(version.Replace("v", ""));
        var current = Version.Parse(fileInfo.FileVersion);
        if (current.CompareTo(v) == 0)
        {
          return true;
        }
        return false;
      }
      catch
      {
        throw new Exception("Could not parse version!");
      }
    }

    private void Yes_Click(object sender, RoutedEventArgs e)
    {
      Process.Start(_path);
      Close();
    }

    private void No_Click(object sender, RoutedEventArgs e)
    {
      Close();
    }
  }
}
