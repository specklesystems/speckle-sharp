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
      Bindings = Locator.Current.GetService<ConnectorBindings>();
      Dictionary<string, string> initialMapping = new Dictionary<string, string>();
      List<string> hostTypes = new List<string>();
      try
      {
        hostTypes = Bindings.GetHostProperties();
        initialMapping = await Task.Run(() => Bindings.GetInitialMapping(state, progress, hostTypes));
      }
      catch
      {

      }

      var vm = new MappingViewModel(initialMapping, hostTypes);
      var mappingView = new MappingView
      {
        DataContext = vm
      };
      vm.OnRequestClose += (s, e) => mappingView.Close();
      mappingView.Show();
    }
    //public Dictionary<string, string> deseralizedSelection(string s)
    //{

    //}
  }
}