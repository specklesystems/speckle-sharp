﻿using Avalonia.Controls;
using Avalonia.Controls.Selection;
using DesktopUI2.Models.Settings;
using DesktopUI2.Views.Settings;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DesktopUI2.ViewModels
{
  public class SettingViewModel : ReactiveObject
  {
    private ISetting _setting;

    public ISetting Setting
    {
      get => _setting;
      set
      {
        this.RaiseAndSetIfChanged(ref _setting, value);
        this.RaisePropertyChanged("Summary");
      }
    }

    public UserControl SettingView { get; private set; }

    private string _selection;
    public string Selection
    {
      get => _selection;
      set
      {
        //sets the selected item on the data model
        Setting.Selection = value;
        this.RaiseAndSetIfChanged(ref _selection, value);

      }
    }

    public SettingViewModel(ISetting setting)
    {
      Setting = setting;
      SettingView = setting.View;

      SettingView.DataContext = this;

      //restores the selected item
      Selection = setting.Selection;
    }


  }
}
