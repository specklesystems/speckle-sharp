using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DesktopUI2.ViewModels;
using System;

namespace DesktopUI2.Views.DataTemplates
{
  public class SettingsTemplateSelector : IDataTemplate
  {


    public IControl Build(object data)
    {
      var svm = data as SettingViewModel;
      var control = (Control)Activator.CreateInstance(svm.Setting.ViewType);
      control.DataContext = svm;
      return control;
    }

    public bool Match(object data)
    {
      return data is SettingViewModel;
    }
  }
}
