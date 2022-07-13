using DesktopUI2.ViewModels;
using DesktopUI2.Views.Settings;
using DesktopUI2.Views.Windows.Dialogs;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Reactive;
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
    public Type ViewType { get; } = typeof(ButtonSettingView);
    public StreamState state { get; set; }
    public ProgressViewModel progress { get; set; }
    public string Summary { get; set; }
    //public ReactiveCommand<Unit, Unit> ButtonCommand { get; set; }

    public async void ButtonCommand()
    {
      Bindings = Locator.Current.GetService<ConnectorBindings>();
      Dictionary<string, string> initialMapping = new Dictionary<string, string>();
      try
      {
        initialMapping = await Task.Run(() => Bindings.GetInitialMapping(state, progress));
      }
      catch
      {

      }

      //User to update logic from computer here;

      //var vm = new MappingViewModel(mappings);
      //var mappingView = new MappingView
      //{
      //  DataContext = vm
      //};

      //mappingView.ShowDialog(MainWindow.Instance);
      //vm.OnRequestClose += (s, e) => mappingView.Close();
      //var newMappings = await mappingView.ShowDialog<Dictionary<string, string>?>(MainWindow.Instance);
      //System.Diagnostics.Debug.WriteLine($"new mappings {newMappings}");


      var vm = new MappingViewModel(initialMapping);
      var mappingView = new MappingView
      {
        DataContext = vm
      };

      mappingView.Show();
    }

  }
}