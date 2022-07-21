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
  public class MappingSeting : ListBoxSetting
  {
    private string _mappingJson = null;
    public string MappingJson
    {
      get => _mappingJson;
      set => _mappingJson = value;
    }
  }
}