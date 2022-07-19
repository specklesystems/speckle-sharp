using Avalonia;
using DesktopUI2.ViewModels;
using DesktopUI2.Views.Settings;
using DesktopUI2.Views.Windows.Dialogs;
using Splat;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DesktopUI2.Models.Settings
{
  public class ButtonSetting : ISetting
  {
    public string Type => typeof(ButtonSetting).ToString();
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Icon { get; set; }
    public ConnectorBindings Bindings { get; set; }
    public string Description { get; set; }
    public Dictionary<string, string> mapping = new Dictionary<string, string>();

    public string ButtonText { get; set; }
    public string Selection { get; set; }

    public string JsonSelection
    {
      get
      {
        string val = Bindings.MappingSelectionValue;
        Selection = val;
        return val;
      }
    }
    public Type ViewType { get; } = typeof(ButtonSettingView);
    public StreamState state { get; set; }
    public ProgressViewModel progress { get; set; }
    public string Summary { get; set; }

    public async void ButtonCommand()
    {
      progress.Report.Log("button command");
      Bindings = Locator.Current.GetService<ConnectorBindings>();
      Dictionary<string, List<KeyValuePair<string,string>>> initialMapping = new Dictionary<string, List<KeyValuePair<string, string>>>();
      List<string> hostTypes = new List<string>();
      Dictionary<string, List<string>> hostTypesDict = new Dictionary<string, List<string>>();
      try
      {
        //hostTypes = Bindings.GetHostProperties();
        hostTypesDict = Bindings.GetHostTypes();
        progress.Report.Log($"host type Dict keys {String.Join(",", hostTypesDict.Keys)}");
        progress.Report.Log($"host type Dict values {String.Join(",", hostTypesDict.Values)}");
        initialMapping = await Task.Run(() => Bindings.GetInitialMapping(state, progress, hostTypesDict));
      }
      catch (Exception ex)
      {
        progress.Report.Log($"Exception occured {ex}");
      }

      var windowOpen = true;
      try
      {
        var vm = new MappingViewModel(initialMapping, hostTypesDict, progress);
        var mappingView = new MappingView
        {
          DataContext = vm
        };
        vm.OnRequestClose += (s, e) =>
        {
          windowOpen = false;
          mappingView.Close();
        };
        ApplicationLifetime
        mappingView.Show();
        while (windowOpen)
        {
          await Task.Delay(TimeSpan.FromMilliseconds(1000));
          //mappingView.Show();
        }
      }
      catch (Exception ex)
      {
        progress.Report.Log($"Exception occured {ex}");
      }
    }
    //public Dictionary<string, string> deseralizedSelection(string s)
    //{

    //}
  }
}