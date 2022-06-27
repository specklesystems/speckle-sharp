using DesktopUI2.Views.Settings;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Windows.Input;

namespace DesktopUI2.Models.Settings
{
  public class MultiSelectBoxSetting : ReactiveObject, ISetting
  {
    public string Type => typeof(MultiSelectBoxSetting).ToString();
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public List<string> Values { get; set; }
    public string Selection { get; set; }
    public string MultiSelectBoxSelection
    {
      set
      {
        if (!Selections.Contains(value))
        {
          Selections.Add(value);
        }
      }
    }
    public ObservableCollection<string> Selections { get; set; }
    private bool _popupVisible;
    public bool PopupVisible
    {
      get => _popupVisible;
      set => this.RaiseAndSetIfChanged(ref _popupVisible, value);
    }
    public ReactiveCommand<Unit, bool> TogglePopup { get; set; }
    public ReactiveCommand<string, Unit> RemoveSelection { get; set; }
    public Type ViewType { get; } = typeof(MultiSelectBoxSettingView);
    public string Summary { get; set; }

  }
}
